import { useCallback, useRef } from 'react';
import { ReadState } from '@/types/chat';

export function useMarkRead() {
  const inflight = useRef<Record<string, boolean>>({});

  const markRead = useCallback(async (chatId: string, atISO?: string): Promise<ReadState | null> => {
    if (inflight.current[chatId]) return null;
    inflight.current[chatId] = true;
    try {
      const res = await fetch(`/api/chats/${chatId}/read`, {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify(atISO ? { at: atISO } : {}),
        credentials: 'include'
      });
      if (!res.ok) return null;
      return await res.json() as ReadState;
    } finally {
      inflight.current[chatId] = false;
    }
  }, []);

  return { markRead };
}