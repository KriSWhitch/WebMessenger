import { NextRequest } from 'next/server';
import { proxyGet } from '@/app/api/utils/proxy';

export async function GET(_req: NextRequest, context: { params: Promise<{ id: string }> }) {
  const { id } = await context.params;
  return proxyGet(`/api/users/profile/${id}`);
}
