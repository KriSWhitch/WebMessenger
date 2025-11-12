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

export async function logoutClient(): Promise<{ success: boolean; error?: string }> {
  try {
    const response = await fetch('/api/auth/logout', {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      const error = await response.json();
      return { success: false, error: error.message || 'Logout failed' };
    }

    if (typeof window !== 'undefined') {
      localStorage.removeItem('user');
      sessionStorage.clear();
      
      window.location.href = '/auth/login';
    }

    return { success: true };
  } catch (error) {
    console.error('Logout error:', error);
    return { success: false, error: 'Network error' };
  }
}