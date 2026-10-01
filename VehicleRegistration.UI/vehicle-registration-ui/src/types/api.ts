// ── Response envelope ───────────────────────────────────────────────────────
export interface ApiResponse<T> {
    success: true;
    data: T;
    message?: string;
}

export interface ApiErrorResponse {
    success: false;
    message: string;
    errors?: Record<string, string[]>;
}

// Custom error thrown by the API client
/*
export class ApiError extends Error {
    constructor(
        message: string,
        public readonly errors?: Record<string, string[]>,
    ) {
        super(message);
    }
}
*/

export class ApiError extends Error {
    public readonly errors?: Record<string, string[]>;

    constructor(message: string, errors?: Record<string, string[]>) {
        super(message);
        this.errors = errors;
    }
}

// ── Vehicles ────────────────────────────────────────────────────────────────
export interface VehicleResponse {
    id: number;
    ownerName: string;
    manufacturerName: string;
    yearOfManufacture: number;
    weight: number;
    categoryName: string;
    categoryIconUrl: string;
}

export interface CreateVehicleRequest {
    ownerId: number;
    manufacturerId: number;
    yearOfManufacture: number;
    weight: number;
}

export interface UpdateVehicleRequest extends CreateVehicleRequest { }

// Approved sort fields — TypeScript prevents sending anything else
export type VehicleSortField = 'ownerName' | 'manufacturer' | 'year' | 'weight';

// ── Categories ──────────────────────────────────────────────────────────────
export interface CategoryResponse {
    id: number;
    name: string;
    minWeight: number;
    maxWeight: number | null;
    iconUrl: string;
}

export interface CreateCategoryRequest {
    name: string;
    minWeight: number;
    maxWeight: number | null;
    iconUrl: string;
}

// Only name and iconUrl — weight ranges are changed via ReplaceCategories
export interface UpdateCategoryRequest {
    name: string;
    iconUrl: string;
}

export interface CategoryItemRequest {
    id?: number;
    name: string;
    minWeight: number;
    maxWeight: number | null;
    iconUrl: string;
}

export interface CategorySetRequest {
    categories: CategoryItemRequest[];
}

export type AbsorbDirection = 'previous' | 'next';

// ── Owners ──────────────────────────────────────────────────────────────────
export interface OwnerResponse {
    id: number;
    name: string;
}

export interface CreateOwnerRequest {
    name: string;
}

// ── Manufacturers ────────────────────────────────────────────────────────────
export interface ManufacturerResponse {
    id: number;
    name: string;
}

export interface CreateManufacturerRequest {
    name: string;
}

export interface LocalCategoryIconUploadResponse {
    iconUrl: string;
}
