'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import Link from 'next/link';
import Image from 'next/image';

interface CreateIncidentRequest {
  envId: string;
  primaryServiceId?: string;
  severity: number;
  title: string;
  detectedAtUnixMs: number;
  createdBy?: string;
  labels?: Record<string, string>;
}

export function CreateIncidentForm() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [formData, setFormData] = useState<CreateIncidentRequest>({
    envId: '00000000-0000-0000-0000-000000000001', // Default environment ID
    primaryServiceId: '',
    severity: 2,
    title: '',
    detectedAtUnixMs: Date.now(),
    createdBy: '',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const createMutation = useMutation({
    mutationFn: async (data: CreateIncidentRequest) => {
      const response = await fetch('/api/incidents', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'x-tenant-id': '00000000-0000-0000-0000-000000000001',
        },
        body: JSON.stringify({
          envId: data.envId,
          primaryServiceId: data.primaryServiceId && data.primaryServiceId.trim() !== '' ? data.primaryServiceId : null,
          severity: data.severity,
          title: data.title,
          detectedAtUnixMs: data.detectedAtUnixMs,
          createdBy: data.createdBy && data.createdBy.trim() !== '' ? data.createdBy : null,
          labels: null,
        }),
      });

      if (!response.ok) {
        const error = await response.json().catch(() => ({ error: 'Failed to create incident' }));
        throw new Error(error.error || 'Failed to create incident');
      }

      return response.json();
    },
    onSuccess: (data) => {
      // Invalidate incidents list to refresh
      queryClient.invalidateQueries({ queryKey: ['incidents'] });
      
      // Extract incident ID from response and redirect
      // Backend returns incident object with Id (capital I) property
      const incidentId = (data as any).Id || (data as any).id;
      if (incidentId) {
        router.push(`/incidents/${incidentId}`);
      } else {
        // Fallback: redirect to home page
        router.push('/');
      }
    },
  });

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.title.trim()) {
      newErrors.title = 'Title is required';
    }

    if (!formData.envId.trim()) {
      newErrors.envId = 'Environment ID is required';
    } else if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(formData.envId)) {
      newErrors.envId = 'Environment ID must be a valid UUID';
    }

    if (formData.primaryServiceId && !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(formData.primaryServiceId)) {
      newErrors.primaryServiceId = 'Service ID must be a valid UUID';
    }

    if (formData.severity < 1 || formData.severity > 4) {
      newErrors.severity = 'Severity must be between 1 and 4';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validate()) {
      return;
    }

    // Convert detected date to Unix timestamp in milliseconds
    const detectedDate = new Date(formData.detectedAtUnixMs);
    const detectedAtUnixMs = detectedDate.getTime();

    createMutation.mutate({
      ...formData,
      detectedAtUnixMs,
    });
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: name === 'severity' ? parseInt(value, 10) : value,
    }));
    // Clear error when user starts typing
    if (errors[name]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[name];
        return newErrors;
      });
    }
  };

  const handleDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const dateValue = e.target.value;
    if (dateValue) {
      const date = new Date(dateValue);
      setFormData((prev) => ({
        ...prev,
        detectedAtUnixMs: date.getTime(),
      }));
    }
  };

  const detectedDateValue = new Date(formData.detectedAtUnixMs).toISOString().slice(0, 16);

  return (
    <main className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto">
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
              <Image
                src="/assets/logo.png"
                alt="IncidentScope Logo"
                width={64}
                height={64}
                className="w-full h-full object-contain"
                unoptimized
              />
            </div>
          </Link>
        </div>

        <div className="bg-white/10 backdrop-blur-xl rounded-2xl shadow-2xl border border-white/20 p-8">
          <h1 className="text-3xl font-bold mb-6 text-white">Create New Incident</h1>

          {createMutation.isError && (
            <div className="mb-4 p-4 bg-red-500/20 backdrop-blur-sm border border-red-400/30 rounded-lg">
              <p className="text-red-200 font-medium">
                {createMutation.error instanceof Error
                  ? createMutation.error.message
                  : 'Failed to create incident'}
              </p>
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-6">
            <div>
              <label htmlFor="title" className="block text-sm font-medium text-gray-200 mb-2">
                Title <span className="text-red-400">*</span>
              </label>
              <input
                type="text"
                id="title"
                name="title"
                value={formData.title}
                onChange={handleChange}
                className={`w-full px-4 py-3 border rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-400/50 text-gray-900 bg-white/95 backdrop-blur-sm shadow-lg transition-all ${
                  errors.title ? 'border-red-400' : 'border-white/30'
                }`}
                placeholder="Enter incident title"
                required
              />
              {errors.title && (
                <p className="mt-1 text-sm text-red-300 font-medium">{errors.title}</p>
              )}
            </div>

            <div>
              <label htmlFor="severity" className="block text-sm font-medium text-gray-200 mb-2">
                Severity <span className="text-red-400">*</span>
              </label>
              <select
                id="severity"
                name="severity"
                value={formData.severity}
                onChange={handleChange}
                className={`w-full px-4 py-3 border rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-400/50 text-gray-900 bg-white/95 backdrop-blur-sm shadow-lg transition-all ${
                  errors.severity ? 'border-red-400' : 'border-white/30'
                }`}
                required
              >
                <option value={1}>P1 - Critical</option>
                <option value={2}>P2 - High</option>
                <option value={3}>P3 - Medium</option>
                <option value={4}>P4 - Low</option>
              </select>
              {errors.severity && (
                <p className="mt-1 text-sm text-red-300 font-medium">{errors.severity}</p>
              )}
            </div>

            <div>
              <label htmlFor="envId" className="block text-sm font-medium text-gray-200 mb-2">
                Environment ID <span className="text-red-400">*</span>
              </label>
              <input
                type="text"
                id="envId"
                name="envId"
                value={formData.envId}
                onChange={handleChange}
                className={`w-full px-4 py-3 border rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-400/50 text-gray-900 bg-white/95 backdrop-blur-sm shadow-lg transition-all ${
                  errors.envId ? 'border-red-400' : 'border-white/30'
                }`}
                placeholder="00000000-0000-0000-0000-000000000001"
                required
              />
              {errors.envId && (
                <p className="mt-1 text-sm text-red-300 font-medium">{errors.envId}</p>
              )}
              <p className="mt-1 text-xs text-gray-400">
                Enter a valid UUID for the environment
              </p>
            </div>

            <div>
              <label htmlFor="primaryServiceId" className="block text-sm font-medium text-gray-200 mb-2">
                Primary Service ID <span className="text-gray-400">(Optional)</span>
              </label>
              <input
                type="text"
                id="primaryServiceId"
                name="primaryServiceId"
                value={formData.primaryServiceId}
                onChange={handleChange}
                className={`w-full px-4 py-3 border rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-400/50 text-gray-900 bg-white/95 backdrop-blur-sm shadow-lg transition-all ${
                  errors.primaryServiceId ? 'border-red-400' : 'border-white/30'
                }`}
                placeholder="00000000-0000-0000-0000-000000000001"
              />
              {errors.primaryServiceId && (
                <p className="mt-1 text-sm text-red-300 font-medium">{errors.primaryServiceId}</p>
              )}
            </div>

            <div>
              <label htmlFor="detectedAt" className="block text-sm font-medium text-gray-200 mb-2">
                Detected At <span className="text-red-400">*</span>
              </label>
              <input
                type="datetime-local"
                id="detectedAt"
                name="detectedAt"
                value={detectedDateValue}
                onChange={handleDateChange}
                className="w-full px-4 py-3 border border-white/30 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-400/50 text-gray-900 bg-white/95 backdrop-blur-sm shadow-lg transition-all"
                required
              />
            </div>

            <div>
              <label htmlFor="createdBy" className="block text-sm font-medium text-gray-200 mb-2">
                Created By <span className="text-gray-400">(Optional)</span>
              </label>
              <input
                type="text"
                id="createdBy"
                name="createdBy"
                value={formData.createdBy}
                onChange={handleChange}
                className="w-full px-4 py-3 border border-white/30 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-400/50 text-gray-900 bg-white/95 backdrop-blur-sm shadow-lg transition-all"
                placeholder="Your name or user ID"
              />
            </div>

            <div className="flex gap-4 pt-4">
              <button
                type="submit"
                disabled={createMutation.isPending}
                className="flex-1 bg-gradient-to-r from-primary-600 to-primary-500 text-white px-6 py-3 rounded-xl hover:from-primary-700 hover:to-primary-600 disabled:opacity-50 disabled:cursor-not-allowed font-medium shadow-lg hover:shadow-xl transition-all transform hover:scale-[1.02]"
              >
                {createMutation.isPending ? 'Creating...' : 'Create Incident'}
              </button>
              <Link
                href="/"
                className="px-6 py-3 border border-white/30 rounded-xl hover:bg-white/10 text-white font-medium backdrop-blur-sm transition-all"
              >
                Cancel
              </Link>
            </div>
          </form>
        </div>
      </div>
    </main>
  );
}

