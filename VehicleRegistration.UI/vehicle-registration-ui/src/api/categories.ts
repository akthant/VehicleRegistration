import apiClient from './apiClient';
import type {
    CategoryResponse,
    CreateCategoryRequest,
    UpdateCategoryRequest,
    CategorySetRequest,
    AbsorbDirection,
    LocalCategoryIconUploadResponse,
} from '../types/api';

export const getCategories = async (): Promise<CategoryResponse[]> => {
    const res = await apiClient.get('/categories');
    return res.data.data;
};

export const createCategory = async (
    body: CreateCategoryRequest,
): Promise<CategoryResponse> => {
    const res = await apiClient.post('/categories', body);
    return res.data.data;
};

export const updateCategory = async (
    id: number,
    body: UpdateCategoryRequest,
): Promise<CategoryResponse> => {
    const res = await apiClient.put(`/categories/${id}`, body);
    return res.data.data;
};

export const replaceCategories = async (
    body: CategorySetRequest,
): Promise<CategoryResponse[]> => {
    const res = await apiClient.put('/categories', body);
    return res.data.data;
};

export const deleteCategory = async (
    id: number,
    absorbInto?: AbsorbDirection,
): Promise<void> => {
    const params = absorbInto ? { absorbInto } : {};
    await apiClient.delete(`/categories/${id}`, { params });
};

export const uploadCategoryIcon = async (
    file: File,
): Promise<LocalCategoryIconUploadResponse> => {
    const response = await fetch('/__local/category-icons', {
        method: 'POST',
        headers: { 'Content-Type': file.type },
        body: file,
    });

    const data: unknown = await response.json().catch(() => null);
    if (!response.ok) {
        const message = typeof data === 'object' && data !== null
            && 'message' in data && typeof data.message === 'string'
            ? data.message
            : 'Icon upload failed.';
        throw new Error(message);
    }

    if (typeof data !== 'object' || data === null
        || !('iconUrl' in data) || typeof data.iconUrl !== 'string') {
        throw new Error('The local upload endpoint returned an invalid response.');
    }

    return { iconUrl: data.iconUrl };
};
