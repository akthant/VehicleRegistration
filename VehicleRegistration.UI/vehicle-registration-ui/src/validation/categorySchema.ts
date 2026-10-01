import { z } from 'zod';

const STEP = 0.01;

const twoDecimalPlaces = (val: number) => /^\d+(\.\d{1,2})?$/.test(String(val));

// Single row
export const categoryItemSchema = z.object({
  id: z.number().int().optional(),
  name: z
    .string()
    .min(1, 'Category name is required.')
    .max(50, 'Category name must not exceed 50 characters.'),
  minWeight: z
      //.number({ invalid_type_error: 'Min weight is required.' })
      .number({ message: 'Min weight is required.' })
    .min(0, 'Min weight must be non-negative.')
    .refine(twoDecimalPlaces, 'Min weight must have at most 2 decimal places.'),
  maxWeight: z
    .number()
    .positive('Max weight must be positive.')
    .refine(twoDecimalPlaces, 'Max weight must have at most 2 decimal places.')
    .nullable(),
    //iconUrl: categoryIconUrlSchema,
  iconUrl: z
    .string()
    .min(1, 'Icon URL is required.')
    .max(500, 'Icon URL must not exceed 500 characters.'),
}).refine(
  (row) => row.maxWeight === null || row.maxWeight > row.minWeight,
  { message: 'Max weight must be greater than min weight.', path: ['maxWeight'] },
);

// Full configuration set — mirrors CategoryRangeRules.Validate on the backend
export const categorySetSchema = z
  .array(categoryItemSchema)
  .min(1, 'At least one category is required.')
  .superRefine((categories, ctx) => {
        if (categories.length === 0) return; // .min(1) already reports this
    const ordered = [...categories].sort((a, b) => a.minWeight - b.minWeight);

    // Must start at 0.00
    if (ordered[0].minWeight !== 0) {
      ctx.addIssue({
        code: 'custom',
        message: `The lightest category ('${ordered[0].name}') must start at 0.00 kg.`,
      });
    }

    // Exactly one unbounded
    const unboundedCount = ordered.filter((c) => c.maxWeight === null).length;
    if (unboundedCount === 0) {
      ctx.addIssue({ code: 'custom', message: 'The heaviest category must have no upper limit (Max Weight = empty).' });
    } else if (unboundedCount > 1) {
      ctx.addIssue({ code: 'custom', message: 'Only one category may have no upper limit.' });
    }

    // Duplicate names
    const names = ordered.map((c) => c.name.trim().toLowerCase());
    const duplicates = names.filter((n, i) => names.indexOf(n) !== i);
    duplicates.forEach((name) =>
      ctx.addIssue({ code: 'custom', message: `Category name '${name}' is used more than once.` }),
    );

    // No gaps or overlaps between consecutive rows
    for (let i = 0; i < ordered.length - 1; i++) {
      const current = ordered[i];
      const next = ordered[i + 1];

      if (current.maxWeight === null) {
        ctx.addIssue({
          code: 'custom',
          message: `'${current.name}' has no upper limit so '${next.name}' cannot follow it.`,
        });
        continue;
      }

      const expectedNextMin = +(current.maxWeight + STEP).toFixed(2);
      if (next.minWeight > expectedNextMin) {
        ctx.addIssue({
          code: 'custom',
          message: `Gap: weights ${expectedNextMin.toFixed(2)} kg to ${(next.minWeight - STEP).toFixed(2)} kg have no category (between '${current.name}' and '${next.name}').`,
        });
      } else if (next.minWeight < expectedNextMin) {
        ctx.addIssue({
          code: 'custom',
          message: `Overlap: '${current.name}' ends at ${current.maxWeight.toFixed(2)} kg but '${next.name}' starts at ${next.minWeight.toFixed(2)} kg.`,
        });
      }
    }
  });

  const bundledCategoryIconUrls = new Set([
    '/icons/light.svg',
    '/icons/medium.svg',
    '/icons/heavy.svg',
]);

export const categoryIconUrlSchema = z
    .string()
    .min(1, 'Icon URL is required.')
    .max(500, 'Icon URL must not exceed 500 characters.')
    .refine(
        (iconUrl) => bundledCategoryIconUrls.has(iconUrl)
            || /^\/icons\/[a-f0-9]{32}\.(?:png|jpg|webp)$/i.test(iconUrl),
        'Choose a bundled icon or upload a PNG, JPEG, or WebP image.',
    );
  
// Form-level schema: matches the form shape { categories: [...] }
export const categoryFormSchema = z.object({
  categories: categorySetSchema,
});

export type CategoryItemFormValues = z.infer<typeof categoryItemSchema>;
export type CategorySetFormValues = z.infer<typeof categoryFormSchema>;