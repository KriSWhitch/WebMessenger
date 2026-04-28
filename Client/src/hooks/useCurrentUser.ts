'use client';

import { useUserContext } from '@/providers/UserProvider';

export function useCurrentUser() {
  return useUserContext();
}
