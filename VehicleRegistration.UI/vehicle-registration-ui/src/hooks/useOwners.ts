import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import * as ownersApi from '../api/owners';
import type { CreateOwnerRequest } from '../types/api';

export const OWNERS_KEY = ['owners'] as const;

export const useOwners = () =>
    useQuery({ queryKey: OWNERS_KEY, queryFn: ownersApi.getOwners });

export const useCreateOwner = () => {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (body: CreateOwnerRequest) => ownersApi.createOwner(body),
        onSuccess: () => qc.invalidateQueries({ queryKey: OWNERS_KEY }),
    });
};