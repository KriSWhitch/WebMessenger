import { NextRequest } from 'next/server';
import { proxyGet, proxyPut } from '@/app/api/utils/proxy';

export async function GET() {
  return proxyGet('/api/users/profile');
}

export async function PUT(req: NextRequest) {
  const body = await req.json().catch(() => ({}));
  return proxyPut('/api/users/profile', body);
}