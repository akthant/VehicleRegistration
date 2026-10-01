import type { VehicleResponse, VehicleSortField } from '../types/api';

// Only approved sort fields — never forward raw strings to the API
const SORTABLE_COLUMNS: { key: VehicleSortField; label: string }[] = [
    { key: 'ownerName', label: 'Owner' },
    { key: 'manufacturer', label: 'Manufacturer' },
    { key: 'year', label: 'Year' },
    { key: 'weight', label: 'Weight (kg)' },
];

interface Props {
    vehicles: VehicleResponse[];
    sortBy?: VehicleSortField;
    desc: boolean;
    onSort: (field: VehicleSortField) => void;
    onEdit: (vehicle: VehicleResponse) => void;
    onDelete: (vehicle: VehicleResponse) => void;
}

export const VehicleTable = ({ vehicles, sortBy, desc, onSort, onEdit, onDelete }: Props) => {
    const SortIcon = ({ field }: { field: VehicleSortField }) => {
        if (sortBy !== field) return <span className="ml-1 text-gray-300" >↕</span>;
        return <span className="ml-1" > { desc? '↓': '↑' } </span>;
    };

    return (
        <table className= "w-full border-collapse text-sm" >
        <thead className="bg-gray-50 text-left" >
            <tr>
            {
                SORTABLE_COLUMNS.map(({ key, label }) => (
                    <th
              key= { key }
              onClick = {() => onSort(key)}
className = "cursor-pointer border-b px-4 py-3 hover:bg-gray-100 select-none"
    >
{ label } < SortIcon field = { key } />
    </th>
          ))}
<th className="border-b px-4 py-3" > Category </th>
    < th className = "border-b px-4 py-3" > Actions </th>
        </tr>
        </thead>
        <tbody>
{
    vehicles.map((v) => (
        <tr key= { v.id } className = "border-b hover:bg-gray-50" >
        <td className="px-4 py-3" > { v.ownerName } </td>
    < td className = "px-4 py-3" > { v.manufacturerName } </td>
    < td className = "px-4 py-3" > { v.yearOfManufacture } </td>
    < td className = "px-4 py-3" > { v.weight.toFixed(2) } </td>
    < td className = "px-4 py-3" >
    <span className="flex items-center gap-2" >
    <img src={ v.categoryIconUrl } alt = { v.categoryName } className = "h-5 w-5" />
    { v.categoryName }
    </span>
    </td>
    < td className = "px-4 py-3" >
    <button onClick={() => onEdit(v)} className = "mr-2 text-blue-600 hover:underline" > Edit </button>
        < button onClick = {() => onDelete(v)} className = "text-red-600 hover:underline" > Delete </button>
            </td>
            </tr>
        ))}
</tbody>
    </table>
  );
};