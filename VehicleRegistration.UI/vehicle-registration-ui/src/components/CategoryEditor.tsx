import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { categorySetSchema } from '../validation/categorySchema';
import type { CategorySetFormValues } from '../validation/categorySchema';
import { useReplaceCategories } from '../hooks/useCategories';
import { FieldError } from './FieldError';
import type { CategoryResponse } from '../types/api';
import { categoryFormSchema } from '../validation/categorySchema';
import { useState } from 'react';
import { uploadCategoryIcon } from '../api/categories';

interface Props {
    categories: CategoryResponse[];
    onSuccess: (message: string) => void;
    onCancel: () => void;
}


export const CategoryEditor = ({ categories, onSuccess, onCancel }: Props) => {
    const replace = useReplaceCategories();

    const { register, control, handleSubmit, setValue, watch,formState: { errors } } = useForm<CategorySetFormValues>({
        resolver: zodResolver(categoryFormSchema),
        defaultValues: { categories },
    });

    const { fields, append, remove } = useFieldArray({ control, name: 'categories' });
    const [uploadingRows, setUploadingRows] = useState<Record<string, boolean>>({});
    const [uploadErrors, setUploadErrors] = useState<Record<string, string>>({});
    const hasPendingUpload = Object.values(uploadingRows).some(Boolean);

    const submit = async (values: CategorySetFormValues) => {
        if (hasPendingUpload) return;
        try {
            await replace.mutateAsync({ categories: values.categories });
            onSuccess('Category configuration saved.');
            onCancel();
        } catch (e: any) {
            alert(e.message ?? 'Failed to save.');
        }
    };


    // Top-level array errors from superRefine
    const arrayErrors = errors.categories?.root?.message
        ?? (errors.categories as any)?.message;

        const allowedTypes = new Set(['image/png', 'image/jpeg', 'image/webp']);
const maxBytes = 2 * 1024 * 1024;

const uploadIconForRow = async (fieldId: string, rowIndex: number, file: File) => {
    setUploadErrors((previous) => ({ ...previous, [fieldId]: '' }));

    if (!allowedTypes.has(file.type)) {
        setUploadErrors((previous) => ({
            ...previous,
            [fieldId]: 'Choose a PNG, JPEG, or WebP image.',
        }));
        return;
    }
    if (file.size > maxBytes) {
        setUploadErrors((previous) => ({
            ...previous,
            [fieldId]: 'Icon images must be 2 MB or smaller.',
        }));
        return;
    }

    setUploadingRows((previous) => ({ ...previous, [fieldId]: true }));
    try {
        const result = await uploadCategoryIcon(file);
        setValue(`categories.${rowIndex}.iconUrl`, result.iconUrl, {
            shouldDirty: true,
            shouldValidate: true,
        });
    } catch (error) {
        setUploadErrors((previous) => ({
            ...previous,
            [fieldId]: error instanceof Error ? error.message : 'Icon upload failed.',
        }));
    } finally {
        setUploadingRows((previous) => ({ ...previous, [fieldId]: false }));
    }
};


    return (
        <form onSubmit={handleSubmit(submit, (errs) => console.log('Validation errors:', errs))}>
        { arrayErrors && (
                <p className="rounded bg-red-50 p-3 text-sm text-red-700" > { arrayErrors } </p>
      )}

<table className="w-full text-sm" >
    <thead>
    <tr className="bg-gray-50 text-left" >
        <th className="px-3 py-2" > Name </th>
            < th className = "px-3 py-2" > Min Weight </th>
                < th className = "px-3 py-2" > Max Weight </th>
                    < th className = "px-3 py-2" > Icon URL </th>
                        < th className = "px-3 py-2" > </th>
                            </tr>
                            </thead>
                            <tbody>
{
    fields.map((field, i) => (
        <tr key= { field.id } className = "border-t" >
        <td className="px-3 py-2" >
    <input { ...register(`categories.${i}.name`) }
                  className = "w-full rounded border px-2 py-1" />
        <FieldError message={ errors.categories?.[i]?.name?.message } />
        </td>
    < td className = "px-3 py-2" >
    <input type="number" step = "0.01"
                  { ...register(`categories.${i}.minWeight`, { valueAsNumber: true }) }
                  className = "w-28 rounded border px-2 py-1" />
        <FieldError message={ errors.categories?.[i]?.minWeight?.message } />
        </td>
    < td className = "px-3 py-2" >
    <input type="number" step = "0.01" placeholder = "(unbounded)"
                  {
            ...register(`categories.${i}.maxWeight`, {
             
                setValueAs: (v) => (v === '' || v == null ? null : Number(v)),
            })
        }
                  className = "w-28 rounded border px-2 py-1" />
        <FieldError message={ errors.categories?.[i]?.maxWeight?.message } />
        </td>
 <td className="px-3 py-2">
    <input type="hidden" {...register(`categories.${i}.iconUrl`)} />
    {watch(`categories.${i}.iconUrl`) && (
        <img
            src={watch(`categories.${i}.iconUrl`)}
            alt={`${watch(`categories.${i}.name`)} icon`}
            className="mb-2 h-10 w-10 object-contain"
        />
    )}
<input
    id={`category-icon-${field.id}`}
    type="file"
    accept="image/png,image/jpeg,image/webp,.png,.jpg,.jpeg,.webp"
    aria-label={`Upload icon for ${watch(`categories.${i}.name`) || `category ${i + 1}`}`}
    disabled={uploadingRows[field.id]}
    onChange={(event) => {
        const file = event.currentTarget.files?.[0];
        event.currentTarget.value = '';
        if (file) void uploadIconForRow(field.id, i, file);
    }}
    className="peer sr-only"
/>
<label
    htmlFor={`category-icon-${field.id}`}
    className="inline-flex cursor-pointer items-center rounded border px-3 py-2 text-sm text-blue-700 hover:bg-blue-50 peer-focus-visible:outline peer-focus-visible:outline-2 peer-focus-visible:outline-blue-600 peer-disabled:cursor-not-allowed peer-disabled:opacity-50"
>
    Choose File
</label>
    {uploadingRows[field.id] && (
        <p role="status" className="mt-1 text-sm text-gray-600">Saving icon...</p>
    )}
    {uploadErrors[field.id] && (
        <p role="alert" className="mt-1 text-sm text-red-600">{uploadErrors[field.id]}</p>
    )}
    <FieldError message={errors.categories?.[i]?.iconUrl?.message} />
</td>

    < td className = "px-3 py-2" >
    <button type="button" className = "text-red-600 hover:underline" onClick={() => remove(i)} disabled={hasPendingUpload}>
    Remove
</button>   
    </td>
    </tr>
          ))}
</tbody>
    </table>

    < button type = "button"
onClick = {() => append({ name: '', minWeight: 0, maxWeight: null, iconUrl: '' })}
className = "text-sm text-blue-600 hover:underline" >
    + Add row
        </button>

        < div className = "flex justify-end gap-2" >
            <button type="button" onClick = { onCancel }
className = "rounded px-4 py-2 text-gray-600 hover:bg-gray-100" >
    Cancel
    </button>
    < button type = "submit" disabled = { replace.isPending }
className = "rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700 disabled:opacity-50" >
{ replace.isPending ? 'Saving…' : 'Save Configuration' }
    </button>
    </div>
    </form>
  );
};