import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { vehicleSchema } from '../validation/vehicleSchema';
import type { VehicleFormValues } from '../validation/vehicleSchema';
import { useOwners, useCreateOwner } from '../hooks/useOwners';
import { useManufacturers } from '../hooks/useManufacturers';
import { useCategories } from '../hooks/useCategories';
import { useCreateVehicle, useUpdateVehicle } from '../hooks/useVehicles';
import { OwnerAutocompleteInput } from './OwnerAutocompleteInput';
import { AddManufacturerModal } from './AddManufacturerModal';
import { FieldError } from './FieldError';
import type { VehicleResponse, ApiError } from '../types/api';

interface Props {
    vehicle?: VehicleResponse; // undefined = create mode
    onClose: () => void;
    onSuccess: (message: string) => void;
}

export const VehicleFormModal = ({ vehicle, onClose, onSuccess }: Props) => {
    const { data: owners = [] } = useOwners();
    const { data: manufacturers = [] } = useManufacturers();
    const { data: categories = [] } = useCategories();
    const createVehicle = useCreateVehicle();
    const updateVehicle = useUpdateVehicle();
    const createOwner = useCreateOwner();

    const [showAddManufacturer, setShowAddManufacturer] = useState(false);
    const [showOwnerCreatePrompt, setShowOwnerCreatePrompt] = useState(false);
    const [pendingSubmit, setPendingSubmit] = useState<VehicleFormValues | null>(null);
    const [apiError, setApiError] = useState('');

    const { register, handleSubmit, watch, setValue, formState: { errors } } = useForm<VehicleFormValues>({
        resolver: zodResolver(vehicleSchema),
        defaultValues: vehicle
            ? {
                ownerName: vehicle.ownerName,
                manufacturerId: manufacturers.find(m => m.name === vehicle.manufacturerName)?.id,
                yearOfManufacture: vehicle.yearOfManufacture,
                weight: vehicle.weight,
            }
            : undefined,
    });

    // Live category preview — no API call, uses already-loaded categories
    const watchedWeight = watch('weight');
    const liveCategory = categories.find(
        (c) => c.minWeight <= watchedWeight && (c.maxWeight === null || c.maxWeight >= watchedWeight),
    );

    const resolveOwner = async (name: string): Promise<number | null> => {
        const existing = owners.find((o) => o.name.toLowerCase() === name.trim().toLowerCase());
        if (existing) return existing.id;
        return null; // signal: needs create prompt
    };

    const submit = async (values: VehicleFormValues) => {
        setApiError('');
        const ownerId = await resolveOwner(values.ownerName);

        if (ownerId === null) {
            // Owner not found — prompt user to confirm creation
            setPendingSubmit(values);
            setShowOwnerCreatePrompt(true);
            return;
        }

        await saveVehicle(values, ownerId);
    };

    const confirmCreateOwner = async () => {
        if (!pendingSubmit) return;
        try {
            const newOwner = await createOwner.mutateAsync({ name: pendingSubmit.ownerName.trim() });
            setShowOwnerCreatePrompt(false);
            await saveVehicle(pendingSubmit, newOwner.id);
        } catch (e) {
            setApiError((e as ApiError).message ?? 'Failed to create owner.');
        }
    };

    const saveVehicle = async (values: VehicleFormValues, ownerId: number) => {
        const body = {
            ownerId,
            manufacturerId: values.manufacturerId,
            yearOfManufacture: values.yearOfManufacture,
            weight: values.weight,
        };
        try {
            if (vehicle) {
                await updateVehicle.mutateAsync({ id: vehicle.id, body });
                onSuccess('Vehicle updated successfully.');
            } else {
                await createVehicle.mutateAsync(body);
                onSuccess('Vehicle created successfully.');
            }
            onClose();
        } catch (e) {
            setApiError((e as ApiError).message ?? 'An error occurred.');
        }
    };

    return (
        <div className= "fixed inset-0 z-50 flex items-center justify-center bg-black/40" >
        <div className="w-full max-w-md rounded bg-white p-6 shadow-xl" >
            <h2 className="mb-4 text-lg font-semibold" >
            { vehicle? 'Edit Vehicle': 'Add Vehicle' }
                </h2>

                < form onSubmit = { handleSubmit(submit) } className = "space-y-4" >

                {/* Owner — text input with autocomplete */ }
                    < div >
                    <label className="block text-sm font-medium" > Owner </label>
                        < OwnerAutocompleteInput
    owners = { owners }
    value = { watch('ownerName') ?? ''
}
onChange = {(name) => setValue('ownerName', name)}
error = { errors.ownerName?.message }
    />
    </div>

{/* Manufacturer — dropdown + Add button */ }
<div>
    <label className="block text-sm font-medium" > Manufacturer </label>
        < div className = "flex gap-2" >
            <select
                { ...register('manufacturerId', { valueAsNumber: true }) }
className = "flex-1 rounded border px-3 py-2"
    >
    <option value="" > Select manufacturer…</option>
{
    manufacturers.map((m) => (
        <option key= { m.id } value = { m.id } > { m.name } </option>
    ))
}
</select>
    < button
type = "button"
onClick = {() => setShowAddManufacturer(true)}
className = "rounded border px-3 py-2 text-blue-600 hover:bg-blue-50"
title = "Add new manufacturer"
    >
                ＋
</button>
    </div>
    < FieldError message = { errors.manufacturerId?.message } />
        </div>

{/* Year */ }
<div>
    <label className="block text-sm font-medium" > Year of Manufacture </label>
        < input
type = "number"
{...register('yearOfManufacture', { valueAsNumber: true }) }
className = "w-full rounded border px-3 py-2"
    />
    <FieldError message={ errors.yearOfManufacture?.message } />
        </div>

{/* Weight with live category preview */ }
<div>
    <label className="block text-sm font-medium" > Weight(kg) </label>
        < input
type = "number"
step = "0.01"
{...register('weight', { valueAsNumber: true }) }
className = "w-full rounded border px-3 py-2"
    />
    <FieldError message={ errors.weight?.message } />
{
    liveCategory && (
        <p className="mt-1 flex items-center gap-1 text-sm text-gray-500" >
            <img src={ liveCategory.iconUrl } alt = "" className = "h-4 w-4" />
                Category: <strong>{ liveCategory.name } </strong>
                    </p>
            )
}
</div>

{/* Owner create prompt */ }
{
    showOwnerCreatePrompt && pendingSubmit && (
        <div className="rounded border border-yellow-300 bg-yellow-50 p-3 text-sm" >
            <p>Owner < strong > "{pendingSubmit.ownerName}" < /strong> not found. Create new owner?</p >
            <div className="mt-2 flex gap-2" >
                <button type="button" onClick = { confirmCreateOwner }
    className = "rounded bg-yellow-500 px-3 py-1 text-white hover:bg-yellow-600" >
        Create
        </button>
        < button type = "button" onClick = {() => setShowOwnerCreatePrompt(false)
}
className = "rounded border px-3 py-1 hover:bg-gray-100" >
    Cancel
    </button>
    </div>
    </div>
          )}

{ apiError && <p className="text-sm text-red-600" > { apiError } </p> }

<div className="flex justify-end gap-2 pt-2" >
    <button type="button" onClick = { onClose }
className = "rounded px-4 py-2 text-gray-600 hover:bg-gray-100" >
    Cancel
    </button>
    < button type = "submit"
className = "rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700" >
{ vehicle? 'Save Changes': 'Add Vehicle' }
    </button>
    </div>
    </form>
    </div>

{
    showAddManufacturer && (
        <AddManufacturerModal
          onClose={ () => setShowAddManufacturer(false) }
    onCreated = {(id) => setValue('manufacturerId', id)
}
        />
      )}
</div>
  );
};