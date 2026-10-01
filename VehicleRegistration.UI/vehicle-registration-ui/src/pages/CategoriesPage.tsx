import { useState } from 'react';
import { useCategories } from '../hooks/useCategories';
import { CategoryEditor } from '../components/CategoryEditor';
import { DeleteCategoryModal } from '../components/DeleteCategoryModal';
import { Toast } from '../components/Toast';
import type { CategoryResponse } from '../types/api';

export const CategoriesPage = () => {
    const { data: categories = [], isLoading } = useCategories();
    //const updateCategory = useUpdateCategory();
    const [showEditor, setShowEditor] = useState(false);
    const [deletingCategory, setDeletingCategory] = useState<CategoryResponse | null>(null);
    const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' } | null>(null);

    const sorted = [...categories].sort((a, b) => a.minWeight - b.minWeight);

    return (
        <div className= "p-6" >
        <div className="mb-4 flex items-center justify-between" >
            <h1 className="text-2xl font-bold" > Vehicle Categories </h1>
                < button
    onClick = {() => setShowEditor(true)}
className = "rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
    >
    Edit Configuration
        </button>
        </div>

{
    isLoading ? <p>Loading…</p> : (
        < table className = "w-full border-collapse text-sm" >
            <thead className="bg-gray-50 text-left" >
                <tr>
                <th className="border-b px-4 py-3" > Icon </th>
                    < th className = "border-b px-4 py-3" > Name </th>
                        < th className = "border-b px-4 py-3" > Min Weight(kg) </th>
                            < th className = "border-b px-4 py-3" > Max Weight(kg) </th>
                                < th className = "border-b px-4 py-3" > Actions </th>
                                    </tr>
                                    </thead>
                                    <tbody>
    {
        sorted.map((cat) => (
            <tr key= { cat.id } className = "border-b hover:bg-gray-50" >
            <td className="px-4 py-3" >
        <img src={ cat.iconUrl } alt = { cat.name } className = "h-6 w-6" />
        </td>
        < td className = "px-4 py-3" > { cat.name } </td>
        < td className = "px-4 py-3" > { cat.minWeight.toFixed(2) } </td>
        < td className = "px-4 py-3" > { cat.maxWeight?.toFixed(2) ?? '∞' } </td>
        < td className = "px-4 py-3" >
        <button onClick={() => setDeletingCategory(cat)} className = "text-red-600 hover:underline">
        Delete
        </button>
        </td>
        </tr>
            ))
}
</tbody>
    </table>
      )}

{
    showEditor && (
        <div className="mt-6 rounded border p-4" >
            <h2 className="mb-4 text-lg font-semibold" > Replace Category Configuration </h2>
                < CategoryEditor
    categories = { categories }
    onSuccess = {(msg) => { setToast({ message: msg, type: 'success' }); }
}
onCancel = {() => setShowEditor(false)}
          />
    </div>
      )}

{
    deletingCategory && (
        <DeleteCategoryModal
          category={ deletingCategory }
    hasPrevious = { sorted.findIndex((c) => c.id === deletingCategory.id) > 0 }
    hasNext = { sorted.findIndex((c) => c.id === deletingCategory.id) < sorted.length - 1 }
    onClose = {() => setDeletingCategory(null)
}
onSuccess = {(msg) => { setToast({ message: msg, type: 'success' }); setDeletingCategory(null); }}
        />
      )}

{ toast && <Toast { ...toast } onDismiss = {() => setToast(null) } />}
</div>
  );
};