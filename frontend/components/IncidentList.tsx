'use client';

import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import { format } from 'date-fns';

interface Incident {
  id: string;
  title: string;
  status: string;
  severity: number;
  createdAt: string;
  detectedAt?: string;
  resolvedAt?: string;
  primaryServiceId?: string;
}

const severityColors = {
  1: 'bg-red-600',
  2: 'bg-orange-600',
  3: 'bg-yellow-600',
  4: 'bg-blue-600',
};

const statusColors = {
  open: 'bg-red-500/30 text-red-200 border border-red-400/30',
  mitigating: 'bg-yellow-500/30 text-yellow-200 border border-yellow-400/30',
  resolved: 'bg-green-500/30 text-green-200 border border-green-400/30',
};

export function IncidentList() {
  const { data: incidents, isLoading, error } = useQuery<Incident[]>({
    queryKey: ['incidents'],
    queryFn: async () => {
      const response = await fetch('/api/incidents', {
        headers: {
          'x-tenant-id': '00000000-0000-0000-0000-000000000001', // Default tenant for dev
        },
      });
      if (!response.ok) {
        throw new Error('Failed to fetch incidents');
      }
      return response.json();
    },
  });

  if (isLoading) {
    return (
      <div className="bg-white/10 backdrop-blur-xl rounded-2xl shadow-2xl border border-white/20 p-8 text-center">
        <p className="text-gray-200 font-medium">Loading incidents...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-500/20 backdrop-blur-xl rounded-2xl shadow-2xl border border-red-400/30 p-6">
        <p className="text-red-200 font-medium">
          Error loading incidents: {error.message}
        </p>
      </div>
    );
  }

  if (!incidents || incidents.length === 0) {
    return (
      <div className="bg-white/10 backdrop-blur-xl rounded-2xl shadow-2xl border border-white/20 p-8 text-center">
        <p className="text-gray-300 font-medium">
          No incidents found. Create one to get started.
        </p>
      </div>
    );
  }

  return (
    <div className="bg-white/10 backdrop-blur-xl rounded-2xl shadow-2xl border border-white/20 overflow-hidden">
      <table className="min-w-full divide-y divide-white/10">
        <thead className="bg-white/5 backdrop-blur-sm">
          <tr>
            <th className="px-6 py-4 text-left text-xs font-semibold text-gray-200 uppercase tracking-wider">
              Title
            </th>
            <th className="px-6 py-4 text-left text-xs font-semibold text-gray-200 uppercase tracking-wider">
              Severity
            </th>
            <th className="px-6 py-4 text-left text-xs font-semibold text-gray-200 uppercase tracking-wider">
              Status
            </th>
            <th className="px-6 py-4 text-left text-xs font-semibold text-gray-200 uppercase tracking-wider">
              Detected
            </th>
            <th className="px-6 py-4 text-left text-xs font-semibold text-gray-200 uppercase tracking-wider">
              Actions
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-white/10">
          {incidents.map((incident) => (
            <tr key={incident.id} className="hover:bg-white/5 transition-colors">
              <td className="px-6 py-4 whitespace-nowrap">
                <Link
                  href={`/incidents/${incident.id}`}
                  className="text-primary-400 hover:text-primary-300 font-medium transition-colors"
                >
                  {incident.title}
                </Link>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <span
                  className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold text-white shadow-lg ${
                    severityColors[incident.severity as keyof typeof severityColors] || 'bg-gray-600'
                  }`}
                >
                  P{incident.severity}
                </span>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <span
                  className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold shadow-lg ${
                    statusColors[incident.status as keyof typeof statusColors] || 'bg-gray-100/20 text-gray-200'
                  }`}
                >
                  {incident.status}
                </span>
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-300">
                {incident.detectedAt
                  ? format(new Date(incident.detectedAt), 'MMM d, yyyy HH:mm')
                  : format(new Date(incident.createdAt), 'MMM d, yyyy HH:mm')}
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                <Link
                  href={`/incidents/${incident.id}`}
                  className="text-primary-400 hover:text-primary-300 transition-colors"
                >
                  View
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

