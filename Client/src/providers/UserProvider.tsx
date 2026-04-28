'use client';

import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';

type UserContextValue = {
  currentUserId: string | null;
  loading: boolean;
};

const UserContext = createContext<UserContextValue | null>(null);

export function UserProvider({ children }: { children: React.ReactNode }) {
  const [currentUserId, setCurrentUserId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const controller = new AbortController();

    (async () => {
      try {
        const r = await fetch('/api/users/profile', {
          cache: 'no-store',
          credentials: 'include',
          signal: controller.signal,
        });
        if (!r.ok) {
          setCurrentUserId(null);
          return;
        }
        const profile = (await r.json()) as { id?: string | null };
        setCurrentUserId(profile?.id ?? null);
      } catch {
        if (!controller.signal.aborted) setCurrentUserId(null);
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    })();

    return () => controller.abort();
  }, []);

  const value = useMemo(() => ({ currentUserId, loading }), [currentUserId, loading]);

  return <UserContext.Provider value={value}>{children}</UserContext.Provider>;
}

export function useUserContext() {
  const ctx = useContext(UserContext);
  if (!ctx) throw new Error('useUserContext must be used within UserProvider');
  return ctx;
}
