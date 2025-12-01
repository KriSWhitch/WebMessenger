import { NextRequest } from 'next/server';
import { proxyPost } from '@/app/api/utils/proxy';

export async function POST(req: NextRequest, context: { params: { userId: string } }) {
  const { userId } = await context.params;
  const body = await req.json().catch(() => ({}));
  return proxyPost(`/api/chats/direct/${userId}/messages`, body);
}
