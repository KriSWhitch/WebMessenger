import { proxyGet } from '@/app/api/utils/proxy';

export async function GET() {
  return proxyGet('/api/auth/verify');
}
