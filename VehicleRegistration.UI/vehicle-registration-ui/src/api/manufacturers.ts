import apiClient from './apiClient';
import type { ManufacturerResponse, CreateManufacturerRequest } from '../types/api';

export const getManufacturers = async (): Promise<ManufacturerResponse[]> => {
    const res = await apiClient.get('/manufacturers');
    return res.data.data;
};

export const createManufacturer = async (
    body: CreateManufacturerRequest,
): Promise<ManufacturerResponse> => {
    const res = await apiClient.post('/manufacturers', body);
    return res.data.data;
};