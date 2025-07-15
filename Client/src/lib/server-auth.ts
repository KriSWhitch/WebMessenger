import { cookies } from 'next/headers';

export async function getServerSession() {
  const cookieStore = await cookies();
  const token = cookieStore.get('auth-token')?.value;
  
  if (!token) return null;

  try {
    const res = await fetch(`${process.env.PUBLIC_API_URL}/api/auth/verify`, {
      cache: 'no-store',
      headers: { 'Authorization': `Bearer ${token}` }
    });
    return res.ok ? { token } : null;
  } catch (error) {
    console.error('Session check failed:', error);
    return null;
  }
}