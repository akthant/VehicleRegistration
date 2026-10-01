import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import * as manufacturersApi from '../api/manufacturers';
import type { CreateManufacturerRequest } from '../types/api';

export const MANUFACTURERS_KEY = ['manufacturers'] as const;

export const useManufacturers = () =>
    useQuery({ queryKey: MANUFACTURERS_KEY, queryFn: manufacturersApi.getManufacturers });

export const useCreateManufacturer = () => {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (body: CreateManufacturerRequest) =>
            manufacturersApi.createManufacturer(body),
        onSuccess: () => qc.invalidateQueries({ queryKey: MANUFACTURERS_KEY }),
    });
};