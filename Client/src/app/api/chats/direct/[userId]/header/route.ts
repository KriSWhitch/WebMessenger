import { NextRequest } from 'next/server';
import { proxyGet } from '@/app/api/utils/proxy';

export async function GET(_req: NextRequest, context: { params: { userId: string } }) {
  const { userId } = await context.params;
  return proxyGet(`/api/chats/direct/${userId}/header`);
}
