import { NextRequest } from 'next/server';
import { proxyGet } from '@/app/api/utils/proxy';

export async function GET(req: NextRequest) {
  const url = new URL(req.url);
  const search = new URLSearchParams(url.search);
  if (!search.has('limit')) search.set('limit', '20');
  return proxyGet('/api/chats', search);
}