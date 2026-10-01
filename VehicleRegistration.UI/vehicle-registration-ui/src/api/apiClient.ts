import axios, { AxiosError } from 'axios';
import { ApiError } from '../types/api';

const apiClient = axios.create({ baseURL: import.meta.env.VITE_API_URL ?? '/api' });

// Covers your API's error format and ASP.NET's default error format (ProblemDetails)
interface ErrorBody {
    message?: string;
    title?: string;
    errors?: Record<string, string[] | string> | null;
}

// Make every errors value an array: { Weight: "x" } becomes { Weight: ["x"] }
const normalizeErrors = (errors?: ErrorBody['errors']): Record<string, string[]> | undefined =>
    errors
        ? Object.fromEntries(
              Object.entries(errors).map(([key, value]) => [key, Array.isArray(value) ? value : [value]]),
          )
        : undefined;

apiClient.interceptors.response.use(
    (response) => response,
    (error: AxiosError<ErrorBody>) => {
        // No response at all: server down, CORS, certificate or network problem
        if (!error.response) {
            throw new ApiError('Unable to reach the server. Please try again later.');
        }

        const data = error.response.data ?? {};
        const errors = normalizeErrors(data.errors);

        // Remove duplicate messages, since one rule can be reported more than once
        const fieldMessages = errors ? [...new Set(Object.values(errors).flat())] : [];

        const message =
            fieldMessages.length > 0
                ? fieldMessages.join(' ')
                : data.message ?? data.title ?? 'An unexpected error occurred.';

        throw new ApiError(message, errors);
    },
);

export default apiClient;