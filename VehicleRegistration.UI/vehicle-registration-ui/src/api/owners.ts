import apiClient from './apiClient';
import type { OwnerResponse, CreateOwnerRequest } from '../types/api';

export const getOwners = async (): Promise<OwnerResponse[]> => {
    const res = await apiClient.get('/owners');
    return res.data.data;
};

export const createOwner = async (
    body: CreateOwnerRequest,
): Promise<OwnerResponse> => {
    const res = await apiClient.post('/owners', body);
    return res.data.data;
};