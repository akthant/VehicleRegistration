import { useState } from 'react';
import type { OwnerResponse } from '../types/api';

interface Props {
    owners: OwnerResponse[];
    value: string;
    onChange: (name: string) => void;
    error?: string;
}

export const OwnerAutocompleteInput = ({ owners, value, onChange, error }: Props) => {
    const [showSuggestions, setShowSuggestions] = useState(false);

    const suggestions = value.length > 0
        ? owners.filter((o) => o.name.toLowerCase().includes(value.toLowerCase()))
        : [];

    return (
        <div className= "relative" >
        <input
        type="text"
    value = { value }
    onChange = {(e) => { onChange(e.target.value); setShowSuggestions(true); }}
onBlur = {() => setTimeout(() => setShowSuggestions(false), 150)}
placeholder = "Type owner name..."
className = "w-full rounded border px-3 py-2"
    />
{ showSuggestions && suggestions.length > 0 && (
        <ul className="absolute z-10 mt-1 w-full rounded border bg-white shadow" >
        {
            suggestions.map((o) => (
                <li
              key= { o.id }
              onMouseDown = {() => onChange(o.name)}
className = "cursor-pointer px-3 py-2 hover:bg-gray-100"
    >
{ o.name }
    </li>
          ))}
</ul>
      )}
{ error && <p className="mt-1 text-sm text-red-600" > { error } </p> }
</div>
  );
};