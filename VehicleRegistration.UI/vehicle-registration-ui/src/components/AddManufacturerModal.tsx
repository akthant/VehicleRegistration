import { useState } from 'react';
import { useCreateManufacturer } from '../hooks/useManufacturers';
import { ApiError } from '../types/api';

interface Props {
    onClose: () => void;
    onCreated: (id: number) => void;
}

export const AddManufacturerModal = ({ onClose, onCreated }: Props) => {
    const [name, setName] = useState('');
    const [error, setError] = useState('');
    const { mutateAsync, isPending } = useCreateManufacturer();

    const handleSubmit = async () => {
        if (!name.trim()) { setError('Manufacturer name is required.'); return; }
        if (name.length > 100) { setError('Name must not exceed 100 characters.'); return; }
        try {
            const created = await mutateAsync({ name: name.trim() });
            onCreated(created.id);
            onClose();
        } catch (e) {
            setError(e instanceof ApiError ? e.message : 'Failed to create manufacturer.');
        }
    };

    return (
        <div className= "fixed inset-0 z-50 flex items-center justify-center bg-black/40" >
        <div className="w-80 rounded bg-white p-6 shadow-xl" >
            <h2 className="mb-4 text-lg font-semibold" > Add Manufacturer </h2>
                < input
    type = "text"
    value = { name }
    onChange = {(e) => setName(e.target.value)}
placeholder = "Manufacturer name"
className = "w-full rounded border px-3 py-2"
    />
{ error && <p className="mt-1 text-sm text-red-600" > { error } </p>}
<div className="mt-4 flex justify-end gap-2" >
    <button onClick={ onClose } className = "rounded px-4 py-2 text-gray-600 hover:bg-gray-100" >
        Cancel
        </button>
        < button
onClick = { handleSubmit }
disabled = { isPending }
className = "rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700 disabled:opacity-50"
    >
{ isPending? 'Adding…': 'Add' }
    </button>
    </div>
    </div>
    </div>
  );
};