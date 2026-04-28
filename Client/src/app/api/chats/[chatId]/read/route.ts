import { NextRequest } from 'next/server';
import { proxyPost } from '@/app/api/utils/proxy';

export async function POST(req: NextRequest, context: { params: Promise<{ chatId: string }> }) {
  const { chatId } = await context.params;

  let body: unknown = {};
  try {
    body = await req.json();
  } catch {
    body = {};
  }

  return proxyPost(`/api/chats/${chatId}/read`, body, true);
}
