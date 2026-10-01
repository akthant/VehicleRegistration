import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
import tailwindcss from '@tailwindcss/vite'
import { randomUUID } from 'node:crypto'
import { mkdir, writeFile } from 'node:fs/promises'
import type { IncomingMessage, ServerResponse } from 'node:http'
import path from 'node:path'
import type { Plugin } from 'vite'

// https://vite.dev/config/
export default defineConfig({
    plugins: [react(), tailwindcss(), localCategoryIconUpload()],
    server: {
        proxy: {
            '/api': {
                target: 'https://localhost:7001',
                changeOrigin: true,
                secure: false, // self-signed cert in dev
            },
        },
    },
})


const MAX_ICON_BYTES = 2 * 1024 * 1024;
const uploadPath = '/__local/category-icons';
const extensionByMimeType: Record<string, string> = {
    'image/png': '.png',
    'image/jpeg': '.jpg',
    'image/webp': '.webp',
};

function json(response: ServerResponse, status: number, body: object) {
    response.statusCode = status;
    response.setHeader('Content-Type', 'application/json; charset=utf-8');
    response.end(JSON.stringify(body));
}

function validSignature(bytes: Buffer, mimeType: string) {
    if (mimeType === 'image/png') {
        return bytes.length >= 8 && bytes.subarray(0, 8).equals(
            Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
        );
    }
    if (mimeType === 'image/jpeg') {
        return bytes.length >= 3 && bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff;
    }
    if (mimeType === 'image/webp') {
        return bytes.length >= 12
            && bytes.toString('ascii', 0, 4) === 'RIFF'
            && bytes.toString('ascii', 8, 12) === 'WEBP';
    }
    return false;
}

function localCategoryIconUpload(): Plugin {
    return {
        name: 'local-category-icon-upload',
        apply: 'serve',
        configureServer(server) {
            const iconsDir = path.resolve(server.config.root, 'public', 'icons');

            server.middlewares.use(uploadPath, (request: IncomingMessage, response: ServerResponse) => {
                if (request.method !== 'POST') {
                    response.setHeader('Allow', 'POST');
                    json(response, 405, { message: 'Method not allowed.' });
                    return;
                }

                const mimeType = request.headers['content-type']?.toLowerCase() ?? '';
                const extension = extensionByMimeType[mimeType];
                if (!extension) {
                    request.resume();
                    json(response, 415, { message: 'Choose a PNG, JPEG, or WebP image.' });
                    return;
                }

                const contentLength = Number(request.headers['content-length'] ?? 0);
                if (contentLength > MAX_ICON_BYTES) {
                    request.resume();
                    json(response, 413, { message: 'Icon images must be 2 MB or smaller.' });
                    return;
                }

                const chunks: Buffer[] = [];
                let size = 0;
                let rejected = false;

                request.on('data', (chunk: Buffer | string) => {
                    if (rejected) return;
                    const bytes = Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk);
                    size += bytes.length;
                    if (size > MAX_ICON_BYTES) {
                        rejected = true;
                        chunks.length = 0;
                        request.resume();
                        json(response, 413, { message: 'Icon images must be 2 MB or smaller.' });
                        return;
                    }
                    chunks.push(bytes);
                });

                request.on('end', async () => {
                    if (rejected) return;
                    const image = Buffer.concat(chunks);
                    if (image.length === 0) {
                        json(response, 400, { message: 'The selected image is empty.' });
                        return;
                    }
                    if (!validSignature(image, mimeType)) {
                        json(response, 400, { message: 'Image content does not match its type.' });
                        return;
                    }

                    const fileName = `${randomUUID()}${extension}`;
                    try {
                        await mkdir(iconsDir, { recursive: true });
                        await writeFile(path.join(iconsDir, fileName), image, { flag: 'wx' });
                        json(response, 201, { iconUrl: `/icons/${fileName}` });
                    } catch (error) {
                        server.config.logger.error(`Could not save category icon: ${String(error)}`);
                        json(response, 500, { message: 'The icon could not be saved.' });
                    }
                });

                request.on('error', (error) => {
                    server.config.logger.error(`Icon upload failed: ${error.message}`);
                    if (!response.headersSent) {
                        json(response, 400, { message: 'The upload was interrupted.' });
                    }
                });
            });
        },
    };
}

