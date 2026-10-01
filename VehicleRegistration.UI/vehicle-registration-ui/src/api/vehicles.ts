import apiClient from './apiClient';
import type {
    VehicleResponse,
    CreateVehicleRequest,
    UpdateVehicleRequest,
    VehicleSortField,
} from '../types/api';

export const getVehicles = async (
    sortBy?: VehicleSortField,
    desc?: boolean,
): Promise<VehicleResponse[]> => {
    const params: Record<string, string | boolean> = {};
    if (sortBy) params.sortBy = sortBy;
    if (desc) params.desc = true;
    const res = await apiClient.get('/vehicles', { params });
    return res.data.data;
};

export const createVehicle = async (
    body: CreateVehicleRequest,
): Promise<VehicleResponse> => {
    const res = await apiClient.post('/vehicles', body);
    return res.data.data;
};

export const updateVehicle = async (
    id: number,
    body: UpdateVehicleRequest,
): Promise<VehicleResponse> => {
    const res = await apiClient.put(`/vehicles/${id}`, body);
    return res.data.data;
};

export const deleteVehicle = async (id: number): Promise<void> => {
    await apiClient.delete(`/vehicles/${id}`);
};