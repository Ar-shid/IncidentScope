'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, useRouter } from 'next/navigation';
import { format } from 'date-fns';
import Link from 'next/link';
import { CreateIncidentForm } from '@/components/CreateIncidentForm';

interface Incident {
  id: string;
  title: string;
  status: string;
  severity: number;
  createdAt: string;
  detectedAt?: string;
  resolvedAt?: string;
  createdBy?: string;
  assignee?: string;
}

export default function IncidentDetailPage() {
  const params = useParams();
  const router = useRouter();
  const queryClient = useQueryClient();
  const incidentId = params.id as string;

  // Handle "new" route - show create form instead of fetching
  if (incidentId === 'new') {
    return <CreateIncidentForm />;
  }

  const { data: incident, isLoading } = useQuery<Incident>({
    queryKey: ['incident', incidentId],
    queryFn: async () => {
      const response = await fetch(`/api/incidents/${incidentId}`, {
        headers: {
          'x-tenant-id': '00000000-0000-0000-0000-000000000001',
        },
      });
      if (!response.ok) {
        throw new Error('Failed to fetch incident');
      }
      return response.json();
    },
  });

  const resolveMutation = useMutation({
    mutationFn: async () => {
      const response = await fetch(`/api/incidents/${incidentId}/resolve`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'x-tenant-id': '00000000-0000-0000-0000-000000000001',
        },
        body: JSON.stringify({ resolvedBy: 'current-user' }),
      });
      if (!response.ok) {
        throw new Error('Failed to resolve incident');
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['incident', incidentId] });
      queryClient.invalidateQueries({ queryKey: ['incidents'] });
    },
  });

  if (isLoading) {
    return (
      <main className="min-h-screen p-8">
        <div className="max-w-7xl mx-auto">
          <div className="bg-white/10 backdrop-blur-xl rounded-2xl shadow-2xl border border-white/20 p-8 text-center">
            <p className="text-gray-200 font-medium">Loading incident...</p>
          </div>
        </div>
      </main>
    );
  }

  if (!incident) {
    return (
      <main className="min-h-screen p-8">
        <div className="max-w-7xl mx-auto">
          <div className="bg-white/10 backdrop-blur-xl rounded-2xl shadow-2xl border border-white/20 p-8 text-center">
            <p className="text-gray-200 font-medium">Incident not found</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-screen p-8">
      <div className="max-w-7xl mx-auto">
        <div className="mb-6 flex items-center justify-between">
          <Link
            href="/"
            className="text-primary-400 hover:text-primary-300 font-medium transition-colors flex items-center gap-2"
          >
            <span>←</span>
            <span>Back to Incidents</span>
          </Link>
          <Link href="/" className="relative w-16 h-16 md:w-20 md:h-20 opacity-90 hover:opacity-100 transition-all hover:scale-105">
            <div className="absolute inset-0 bg-white/5 rounded-xl backdrop-blur-sm border border-white/10 shadow-lg flex items-center justify-center p-2">
              <img
                src="/assets/logo.png"
                alt="IncidentScope Logo"
                className="w-full h-full object-contain"
              />
            </div>
          </Link>
        </div>

        <div className="bg-white/10 backdrop-blur-xl rounded-2xl shadow-2xl border border-white/20 p-8">
          <div className="flex justify-between items-start mb-6">
            <div>
              <h1 className="text-3xl font-bold mb-2 text-white">{incident.title}</h1>
              <div className="flex gap-4 text-sm text-gray-300">
                <span>Status: {incident.status}</span>
                <span>Severity: P{incident.severity}</span>
                {incident.createdBy && <span>Created by: {incident.createdBy}</span>}
              </div>
            </div>
            {incident.status !== 'resolved' && (
              <button
                onClick={() => resolveMutation.mutate()}
                disabled={resolveMutation.isPending}
                className="bg-gradient-to-r from-green-600 to-green-500 text-white px-6 py-3 rounded-xl hover:from-green-700 hover:to-green-600 disabled:opacity-50 shadow-lg hover:shadow-xl transition-all transform hover:scale-105"
              >
                {resolveMutation.isPending ? 'Resolving...' : 'Mark Resolved'}
              </button>
            )}
          </div>

          <div className="border-t border-white/20 pt-6">
            <h2 className="text-xl font-semibold mb-4 text-white">Details</h2>
            <dl className="grid grid-cols-2 gap-4">
              <div>
                <dt className="text-sm font-medium text-gray-300">Detected At</dt>
                <dd className="mt-1 text-sm text-white font-medium">
                  {incident.detectedAt
                    ? format(new Date(incident.detectedAt), 'PPpp')
                    : format(new Date(incident.createdAt), 'PPpp')}
                </dd>
              </div>
              {incident.resolvedAt && (
                <div>
                  <dt className="text-sm font-medium text-gray-300">Resolved At</dt>
                  <dd className="mt-1 text-sm text-white font-medium">
                    {format(new Date(incident.resolvedAt), 'PPpp')}
                  </dd>
                </div>
              )}
            </dl>
          </div>

          <div className="border-t border-white/20 pt-6 mt-6">
            <h2 className="text-xl font-semibold mb-4 text-white">Timeline</h2>
            <p className="text-gray-300">Timeline will be populated when correlation engine is implemented.</p>
          </div>

          <div className="border-t border-white/20 pt-6 mt-6">
            <h2 className="text-xl font-semibold mb-4 text-white">Hypotheses</h2>
            <p className="text-gray-300">Root cause hypotheses will appear here when correlation engine is implemented.</p>
          </div>
        </div>
      </div>
    </main>
  );
}

