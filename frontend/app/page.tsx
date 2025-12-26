import Link from 'next/link';
import { IncidentList } from '@/components/IncidentList';

export default function Home() {
  return (
    <main className="min-h-screen p-8">
      <div className="max-w-7xl mx-auto">
        <div className="mb-8">
          <h1 className="text-4xl font-bold mb-2">IncidentScope</h1>
          <p className="text-gray-600">
            Production-ready observability and incident management
          </p>
        </div>

        <div className="mb-6">
          <Link
            href="/incidents/new"
            className="bg-primary-600 text-white px-4 py-2 rounded-lg hover:bg-primary-700"
          >
            Create Incident
          </Link>
        </div>

        <IncidentList />
      </div>
    </main>
  );
}

