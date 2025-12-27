import Link from 'next/link';
import { IncidentList } from '@/components/IncidentList';

export default function Home() {
  return (
    <main className="min-h-screen p-8">
      <div className="max-w-7xl mx-auto">
        <div className="mb-8 flex items-center gap-6">
          <div className="relative w-20 h-20 md:w-24 md:h-24 flex-shrink-0">
            <div className="absolute inset-0 bg-white/5 rounded-2xl backdrop-blur-sm border border-white/10 shadow-lg flex items-center justify-center p-3">
            <img
              src="/assets/logo.png"
              alt="IncidentScope Logo"
              className="w-full h-full object-contain"
            />
            </div>
          </div>
          <div>
            <h1 className="text-5xl font-bold mb-3 text-white bg-gradient-to-r from-white to-gray-300 bg-clip-text text-transparent">
              IncidentScope
            </h1>
            <p className="text-gray-300 text-lg">
              Production-ready observability and incident management
            </p>
          </div>
        </div>

        <div className="mb-6">
          <Link
            href="/incidents/new"
            className="inline-block bg-gradient-to-r from-primary-600 to-primary-500 text-white px-6 py-3 rounded-xl hover:from-primary-700 hover:to-primary-600 font-medium shadow-lg hover:shadow-xl transition-all transform hover:scale-105"
          >
            Create Incident
          </Link>
        </div>

        <IncidentList />
      </div>
    </main>
  );
}

