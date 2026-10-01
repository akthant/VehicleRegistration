import { useState } from 'react';
import { useVehicles, useDeleteVehicle } from '../hooks/useVehicles';
import { VehicleTable } from '../components/VehicleTable';
import { VehicleFormModal } from '../components/VehicleFormModal';
import { Toast } from '../components/Toast';
import type { VehicleResponse, VehicleSortField } from '../types/api';

export const VehiclesPage = () => {
    const [sortBy, setSortBy] = useState<VehicleSortField | undefined>();
    const [desc, setDesc] = useState(false);
    const [editingVehicle, setEditingVehicle] = useState<VehicleResponse | null>(null);
    const [showForm, setShowForm] = useState(false);
    const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' } | null>(null);

    const { data: vehicles = [], isLoading } = useVehicles(sortBy, desc);
    const deleteVehicle = useDeleteVehicle();

    const handleSort = (field: VehicleSortField) => {
        if (sortBy === field) setDesc((d) => !d);
        else { setSortBy(field); setDesc(false); }
    };

    const handleDelete = async (vehicle: VehicleResponse) => {
        if (!confirm(`Delete vehicle owned by ${vehicle.ownerName}?`)) return;
        try {
            await deleteVehicle.mutateAsync(vehicle.id);
            setToast({ message: 'Vehicle deleted.', type: 'success' });
        } catch {
            setToast({ message: 'Failed to delete vehicle.', type: 'error' });
        }
    };

    return (
        <div className= "p-6" >
        <div className="mb-4 flex items-center justify-between" >
            <h1 className="text-2xl font-bold" > Vehicles </h1>
                < button
    onClick = {() => { setEditingVehicle(null); setShowForm(true); }}
className = "rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
    >
    + Add Vehicle
        </button>
        </div>

{
    isLoading ? (
        <p>Loading…</p>
      ) : (
        <VehicleTable
          vehicles= { vehicles }
    sortBy = { sortBy }
    desc = { desc }
    onSort = { handleSort }
    onEdit = {(v) => { setEditingVehicle(v); setShowForm(true); }
}
onDelete = { handleDelete }
    />
      )}

{
    showForm && (
        <VehicleFormModal
          vehicle={ editingVehicle ?? undefined }
    onClose = {() => setShowForm(false)
}
onSuccess = {(msg) => setToast({ message: msg, type: 'success' })}
        />
      )}

{ toast && <Toast { ...toast } onDismiss = {() => setToast(null) } />}
</div>
  );
};