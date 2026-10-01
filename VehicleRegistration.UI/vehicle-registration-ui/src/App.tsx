import { useState } from 'react';
import { VehiclesPage } from './pages/VehiclesPage';
import { CategoriesPage } from './pages/CategoriesPage';

type Tab = 'vehicles' | 'categories';

export default function App() {
  const [tab, setTab] = useState<Tab>('vehicles');

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow">
        <div className="mx-auto flex max-w-5xl gap-4 px-6 py-3">
          <span className="mr-4 text-lg font-bold text-blue-700">Vehicle Registration</span>
          {(['vehicles', 'categories'] as Tab[]).map((t) => (
            <button
              key={t}
              onClick={() => setTab(t)}
              className={`capitalize rounded px-3 py-1 ${
                tab === t
                  ? 'bg-blue-600 text-white'
                  : 'text-gray-600 hover:bg-gray-100'
              }`}
            >
              {t}
            </button>
          ))}
        </div>
      </nav>
      <main className="mx-auto max-w-5xl">
        {tab === 'vehicles' ? <VehiclesPage /> : <CategoriesPage />}
      </main>
    </div>
  );
}