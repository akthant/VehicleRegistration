import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import * as vehiclesApi from '../api/vehicles';
import type { VehicleSortField, CreateVehicleRequest, UpdateVehicleRequest } from '../types/api';

export const VEHICLES_KEY = ['vehicles'] as const;

export const useVehicles = (sortBy?: VehicleSortField, desc?: boolean) =>
    useQuery({
        queryKey: [...VEHICLES_KEY, sortBy, desc],
        queryFn: () => vehiclesApi.getVehicles(sortBy, desc),
    });

export const useCreateVehicle = () => {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (body: CreateVehicleRequest) => vehiclesApi.createVehicle(body),
        onSuccess: () => qc.invalidateQueries({ queryKey: VEHICLES_KEY }),
    });
};

export const useUpdateVehicle = () => {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: ({ id, body }: { id: number; body: UpdateVehicleRequest }) =>
            vehiclesApi.updateVehicle(id, body),
        onSuccess: () => qc.invalidateQueries({ queryKey: VEHICLES_KEY }),
    });
};

export const useDeleteVehicle = () => {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (id: number) => vehiclesApi.deleteVehicle(id),
        onSuccess: () => qc.invalidateQueries({ queryKey: VEHICLES_KEY }),
    });
};