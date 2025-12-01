import { NextRequest } from 'next/server';
import { proxyGet } from '@/app/api/utils/proxy';

export async function GET(_req: NextRequest, context: { params: { chatId: string } }) {
  const { chatId } = await context.params;
  return proxyGet(`/api/chats/${encodeURIComponent(chatId)}/header`);
}
