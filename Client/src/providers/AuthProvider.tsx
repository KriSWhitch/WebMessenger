'use client';
import { usePathname, useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { checkAuthClient } from '@/lib/client-auth';

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const [isLoading, setIsLoading] = useState(true);
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    const verifyAuth = async () => {
      try {
        const authStatus = await checkAuthClient();
        setIsAuthenticated(authStatus);
        
        if (!authStatus && !pathname.startsWith('/auth')) {
          router.replace('/auth/login');
          return;
        }
        
        if (authStatus && pathname.startsWith('/auth')) {
          router.replace('/');
          return;
        }
      } catch (error) {
        console.error('Auth check error:', error);
        if (!pathname.startsWith('/auth')) {
          router.replace('/auth/login');
        }
      } finally {
        setIsLoading(false);
      }
    };

    verifyAuth();
  }, [pathname, router]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-dark-green-900">
        <div className="text-green-400">Checking authentication...</div>
      </div>
    );
  }

  if (!isAuthenticated && !pathname.startsWith('/auth')) {
    return null;
  }

  return <>{children}</>;
}