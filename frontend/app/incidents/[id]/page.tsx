'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, useRouter } from 'next/navigation';
import { format } from 'date-fns';
import Link from 'next/link';

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
    return <div className="p-8">Loading incident...</div>;
  }

  if (!incident) {
    return <div className="p-8">Incident not found</div>;
  }

  return (
    <main className="min-h-screen p-8">
      <div className="max-w-7xl mx-auto">
        <div className="mb-6">
          <Link
            href="/"
            className="text-primary-600 hover:text-primary-800"
          >
            ← Back to Incidents
          </Link>
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex justify-between items-start mb-6">
            <div>
              <h1 className="text-3xl font-bold mb-2">{incident.title}</h1>
              <div className="flex gap-4 text-sm text-gray-600">
                <span>Status: {incident.status}</span>
                <span>Severity: P{incident.severity}</span>
                {incident.createdBy && <span>Created by: {incident.createdBy}</span>}
              </div>
            </div>
            {incident.status !== 'resolved' && (
              <button
                onClick={() => resolveMutation.mutate()}
                disabled={resolveMutation.isPending}
                className="bg-green-600 text-white px-4 py-2 rounded-lg hover:bg-green-700 disabled:opacity-50"
              >
                {resolveMutation.isPending ? 'Resolving...' : 'Mark Resolved'}
              </button>
            )}
          </div>

          <div className="border-t pt-6">
            <h2 className="text-xl font-semibold mb-4">Details</h2>
            <dl className="grid grid-cols-2 gap-4">
              <div>
                <dt className="text-sm font-medium text-gray-500">Detected At</dt>
                <dd className="mt-1 text-sm text-gray-900">
                  {incident.detectedAt
                    ? format(new Date(incident.detectedAt), 'PPpp')
                    : format(new Date(incident.createdAt), 'PPpp')}
                </dd>
              </div>
              {incident.resolvedAt && (
                <div>
                  <dt className="text-sm font-medium text-gray-500">Resolved At</dt>
                  <dd className="mt-1 text-sm text-gray-900">
                    {format(new Date(incident.resolvedAt), 'PPpp')}
                  </dd>
                </div>
              )}
            </dl>
          </div>

          <div className="border-t pt-6 mt-6">
            <h2 className="text-xl font-semibold mb-4">Timeline</h2>
            <p className="text-gray-600">Timeline will be populated when correlation engine is implemented.</p>
          </div>

          <div className="border-t pt-6 mt-6">
            <h2 className="text-xl font-semibold mb-4">Hypotheses</h2>
            <p className="text-gray-600">Root cause hypotheses will appear here when correlation engine is implemented.</p>
          </div>
        </div>
      </div>
    </main>
  );
}

