export async function checkAuthClient(): Promise<boolean> {
  try {
    const res = await fetch(`/api/auth/verify`, {
      cache: 'no-store',
      credentials: 'include'
    });
    return res.ok;
  } catch (error) {
    console.error('Auth check failed:', error);
    return false;
  }
}

export async function logoutClient(): Promise<void> {
  try {
    await fetch('/api/auth/logout', { 
      method: 'POST',
      credentials: 'include'
    });
  } catch (error) {
    console.error('Logout failed:', error);
    throw error;
  }
}