import { NextRequest } from 'next/server';
import { proxyGet } from '@/app/api/utils/proxy';

export async function GET(req: NextRequest, context: { params: { chatId: string } }) {
  const { chatId } = await context.params;
  const url = new URL(req.url);
  const search = new URLSearchParams(url.search);
  return proxyGet(`/api/chats/${chatId}/messages`, search);
}
