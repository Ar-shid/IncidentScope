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
  open: 'bg-red-100 text-red-800',
  mitigating: 'bg-yellow-100 text-yellow-800',
  resolved: 'bg-green-100 text-green-800',
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
    return <div className="text-gray-600">Loading incidents...</div>;
  }

  if (error) {
    return (
      <div className="text-red-600">
        Error loading incidents: {error.message}
      </div>
    );
  }

  if (!incidents || incidents.length === 0) {
    return (
      <div className="text-gray-600">
        No incidents found. Create one to get started.
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg shadow overflow-hidden">
      <table className="min-w-full divide-y divide-gray-200">
        <thead className="bg-gray-50">
          <tr>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Title
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Severity
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Status
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Detected
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Actions
            </th>
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-200">
          {incidents.map((incident) => (
            <tr key={incident.id} className="hover:bg-gray-50">
              <td className="px-6 py-4 whitespace-nowrap">
                <Link
                  href={`/incidents/${incident.id}`}
                  className="text-primary-600 hover:text-primary-800 font-medium"
                >
                  {incident.title}
                </Link>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <span
                  className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium text-white ${
                    severityColors[incident.severity as keyof typeof severityColors] || 'bg-gray-600'
                  }`}
                >
                  P{incident.severity}
                </span>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <span
                  className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                    statusColors[incident.status as keyof typeof statusColors] || 'bg-gray-100 text-gray-800'
                  }`}
                >
                  {incident.status}
                </span>
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {incident.detectedAt
                  ? format(new Date(incident.detectedAt), 'MMM d, yyyy HH:mm')
                  : format(new Date(incident.createdAt), 'MMM d, yyyy HH:mm')}
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                <Link
                  href={`/incidents/${incident.id}`}
                  className="text-primary-600 hover:text-primary-900"
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

