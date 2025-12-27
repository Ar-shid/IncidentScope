import { NextRequest, NextResponse } from 'next/server';

const API_GATEWAY_URL = process.env.NEXT_PUBLIC_API_GATEWAY_URL || 'http://localhost:5000';

export async function GET(request: NextRequest) {
  const tenantId = request.headers.get('x-tenant-id') || '00000000-0000-0000-0000-000000000001';
  
  const response = await fetch(`${API_GATEWAY_URL}/api/incidents`, {
    headers: {
      'x-tenant-id': tenantId,
    },
    cache: 'no-store',
  });

  if (!response.ok) {
    return NextResponse.json(
      { error: 'Failed to fetch incidents' },
      { status: response.status }
    );
  }

  const data = await response.json();
  return NextResponse.json(data);
}

export async function POST(request: NextRequest) {
  const tenantId = request.headers.get('x-tenant-id') || '00000000-0000-0000-0000-000000000001';
  const body = await request.json();
  
  const response = await fetch(`${API_GATEWAY_URL}/api/incidents`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'x-tenant-id': tenantId,
    },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    return NextResponse.json(
      { error: 'Failed to create incident' },
      { status: response.status }
    );
  }

  const data = await response.json();
  return NextResponse.json(data, { status: response.status });
}

