import React from 'react';

interface FieldErrorProps {
    message?: string;
    apiErrors?: Record<string, string[]>;
    field?: string;
}

export const FieldError = ({ message, apiErrors, field }: FieldErrorProps) => {
    const apiMessage = field ? apiErrors?.[field]?.[0] : undefined;
    const text = message ?? apiMessage;
    if (!text) return null;
    return <p className="mt-1 text-sm text-red-600">{text}</p>;
};

interface ToastProps {
    message: string;
    type?: 'success' | 'error';
    onDismiss?: () => void;
}

export const Toast = ({ message, type = 'success', onDismiss }: ToastProps) => {
    const bg = type === 'success' ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800';
    return (
        <div className={`fixed bottom-6 right-6 max-w-sm rounded px-4 py-2 shadow ${bg}`} role="status">
            <div className="flex items-center justify-between gap-4">
                <div className="text-sm">{message}</div>
                <button type="button" onClick={onDismiss} className="ml-2 rounded px-2 py-1 text-sm font-medium">
                    ✕
                </button>
            </div>
        </div>
    );
};