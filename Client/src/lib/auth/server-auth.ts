import { cookies } from 'next/headers';

export async function getServerSession() {
  const cookieStore = await cookies();
  const token = cookieStore.get('auth-token')?.value;

  if (!token) return null;

  const apiBaseUrl = process.env.API_BASE_URL ?? process.env.PUBLIC_API_URL;
  if (!apiBaseUrl) {
    console.error('API_BASE_URL or PUBLIC_API_URL is not configured');
    return null;
  }

  try {
    const res = await fetch(`${apiBaseUrl}/api/auth/verify`, {
      cache: 'no-store',
      headers: { Authorization: `Bearer ${token}` },
    });
    return res.ok ? { token } : null;
  } catch (error) {
    console.error('Session check failed:', error);
    return null;
  }
}
