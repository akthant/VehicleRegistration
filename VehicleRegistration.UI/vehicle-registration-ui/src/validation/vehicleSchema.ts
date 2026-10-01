import { z } from 'zod';

const currentYear = new Date().getFullYear();

// Reusable check: max 2 decimal places
const twoDecimalPlaces = (val: number) => /^\d+(\.\d{1,2})?$/.test(String(val));

export const vehicleSchema = z.object({
    ownerName: z
        .string({ message: 'Owner name is required.' })
        .trim()
        .min(1, 'Owner name is required.')
        .max(100, 'Owner name must not exceed 100 characters.'),

    manufacturerId: z
        .number({ message: 'Manufacturer is required.' })
        .int()
        .positive('Manufacturer is required.'),

    yearOfManufacture: z
        .number({ message: 'Year is required.' })
        .int()
        .min(1886, 'Year must be 1886 or later.')
        .max(currentYear + 1, `Year must not exceed ${currentYear + 1}.`),

    weight: z
        .number({ message: 'Weight is required.' })
        .min(0, 'Weight must be a non-negative value.')
        .refine(twoDecimalPlaces, 'Weight must have at most 2 decimal places.'),
});

export type VehicleFormValues = z.infer<typeof vehicleSchema>;