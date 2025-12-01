import { NextRequest } from 'next/server';
import { proxyPost } from '@/app/api/utils/proxy';

export async function POST(req: NextRequest) {
  const form = await req.formData();
  return proxyPost('/api/users/avatar', form);
}
