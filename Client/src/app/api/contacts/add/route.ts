import { NextRequest } from 'next/server';
import { proxyPost } from '@/app/api/utils/proxy';

export async function POST(req: NextRequest) {
  const body = await req.json().catch(() => ({}));
  return proxyPost('/api/contacts/add', body);
}