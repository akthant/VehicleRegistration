import { useDeleteCategory } from '../hooks/useCategories';
import { ApiError } from '../types/api';
import type { CategoryResponse, AbsorbDirection } from '../types/api';

interface Props {
    category: CategoryResponse;
    hasPrevious: boolean;
    hasNext: boolean;
    onClose: () => void;
    onSuccess: (message: string) => void;
}

export const DeleteCategoryModal = ({ category, hasPrevious, hasNext, onClose, onSuccess }: Props) => {
    const deleteCategory = useDeleteCategory();

    // No neighbour to take over this range: it's the only category left
    const isLastCategory = !hasPrevious && !hasNext;

    const handleDelete = async (absorbInto: AbsorbDirection) => {
        try {
            await deleteCategory.mutateAsync({ id: category.id, absorbInto });
            onSuccess(`'${category.name}' deleted.`);
            onClose();
        } catch (e) {
            alert(e instanceof ApiError ? e.message : 'Delete failed.');
        }
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
            <div className="w-96 rounded bg-white p-6 shadow-xl">
                <h2 className="mb-2 text-lg font-semibold">Delete "{category.name}"</h2>

                {isLastCategory ? (
                    <>
                        <p className="mb-4 rounded bg-red-50 p-3 text-sm text-red-700">
                            The last remaining category cannot be deleted.
                        </p>
                        <div className="flex justify-end">
                            <button onClick={onClose} className="rounded border px-4 py-2 hover:bg-gray-100">
                                Close
                            </button>
                        </div>
                    </>
                ) : (
                    <>
                        <p className="mb-4 text-sm text-gray-600">
                            Deleting this category would create a gap. Choose which adjacent category takes over its range:
                        </p>
                        <div className="flex flex-col gap-2">
                            {hasPrevious && (
                                <button
                                    onClick={() => handleDelete('previous')}
                                    disabled={deleteCategory.isPending}
                                    className="rounded bg-red-600 px-4 py-2 text-white hover:bg-red-700 disabled:opacity-50"
                                >
                                    ← Takes over to Previous Category
                                </button>
                            )}
                            {hasNext && (
                                <button
                                    onClick={() => handleDelete('next')}
                                    disabled={deleteCategory.isPending}
                                    className="rounded bg-red-600 px-4 py-2 text-white hover:bg-red-700 disabled:opacity-50"
                                >
                                    Takes over to Next Category →
                                </button>
                            )}
                            <button onClick={onClose} className="rounded border px-4 py-2 hover:bg-gray-100">
                                Cancel
                            </button>
                        </div>
                    </>
                )}
            </div>
        </div>
    );
};