interface FieldErrorProps {
    // From React Hook Form
    message?: string;
    // From API error envelope errors dict — e.g. errors['Weight'][0]
    apiErrors?: Record<string, string[]>;
    field?: string;
}

export const FieldError = ({ message, apiErrors, field }: FieldErrorProps) => {
    const apiMessage = field ? apiErrors?.[field]?.[0] : undefined;
    const text = message ?? apiMessage;
    if (!text) return null;
    return <p className="mt-1 text-sm text-red-600" > { text } </p>;
};