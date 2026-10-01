import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import * as categoriesApi from '../api/categories';
import { VEHICLES_KEY } from './useVehicles';
import type {
    CreateCategoryRequest,
    UpdateCategoryRequest,
    CategorySetRequest,
    AbsorbDirection,
} from '../types/api';

export const CATEGORIES_KEY = ['categories'] as const;

const invalidateBoth = (qc: ReturnType<typeof useQueryClient>) => {
    qc.invalidateQueries({ queryKey: CATEGORIES_KEY });
    qc.invalidateQueries({ queryKey: VEHICLES_KEY }); // instant category reflection
};

export const useCategories = () =>
    useQuery({
        queryKey: CATEGORIES_KEY,
        queryFn: categoriesApi.getCategories,
    });

export const useCreateCategory = () => {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (body: CreateCategoryRequest) => categoriesApi.createCategory(body),
        onSuccess: () => invalidateBoth(qc),
    });
};

export const useUpdateCategory = () => {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: ({ id, body }: { id: number; body: UpdateCategoryRequest }) =>
            categoriesApi.updateCategory(id, body),
        onSuccess: () => invalidateBoth(qc),
    });
};

export const useReplaceCategories = () => {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (body: CategorySetRequest) => categoriesApi.replaceCategories(body),
        onSuccess: () => invalidateBoth(qc),
    });
};

export const useDeleteCategory = () => {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: ({ id, absorbInto }: { id: number; absorbInto?: AbsorbDirection }) =>
            categoriesApi.deleteCategory(id, absorbInto),
        onSuccess: () => invalidateBoth(qc),
    });
};