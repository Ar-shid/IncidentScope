import { NextRequest, NextResponse } from 'next/server';

const API_GATEWAY_URL = process.env.NEXT_PUBLIC_API_GATEWAY_URL || 'http://localhost:5000';

export async function POST(
  request: NextRequest,
  { params }: { params: { id: string } }
) {
  const tenantId = request.headers.get('x-tenant-id') || '00000000-0000-0000-0000-000000000001';
  const body = await request.json();
  
  const response = await fetch(`${API_GATEWAY_URL}/api/incidents/${params.id}/resolve`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'x-tenant-id': tenantId,
    },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    if (response.status === 404) {
      return NextResponse.json(
        { error: 'Incident not found' },
        { status: 404 }
      );
    }
    return NextResponse.json(
      { error: 'Failed to resolve incident' },
      { status: response.status }
    );
  }

  const data = await response.json();
  return NextResponse.json(data);
}

