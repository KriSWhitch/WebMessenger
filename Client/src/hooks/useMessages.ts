'use client';
import { useCallback, useEffect, useRef, useState } from 'react';
import type { MessageVM } from '@/components/features/messenger/chat/MessageBubble';
import { byDateAsc, mergeUniqueById, normalizePage } from '@/lib/utils/pagination';

type PageResponse = {
  items: MessageVM[];
  hasMore: boolean;
  nextBefore?: string | null;
};

const bySentAtAsc = byDateAsc<MessageVM>((m) => m.sentAt);

function sortAsc(arr: MessageVM[]) {
  return [...arr].sort(bySentAtAsc);
}

export function useMessages(opts: {
  chatId: string | null;
  pageSize?: number;
  meId?: string | null;
}) {
  const { chatId, pageSize = 30, meId } = opts;

  const [messages, setMessages] = useState<MessageVM[]>([]);
  const [loading, setLoading] = useState(false);
  const [hasMore, setHasMore] = useState(false);
  const [nextBefore, setNextBefore] = useState<string | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);

  const scrollToBottom = useCallback(() => {
    const el = containerRef.current;
    if (!el) return;
    el.scrollTop = el.scrollHeight;
  }, []);

  const isNearBottom = useCallback(() => {
    const el = containerRef.current;
    if (!el) return true;
    const threshold = 120;
    return el.scrollHeight - el.scrollTop - el.clientHeight <= threshold;
  }, []);

  useEffect(() => {
    setMessages([]);
    setHasMore(false);
    setNextBefore(null);
  }, [chatId]);

  useEffect(() => {
    if (!chatId) return;
    let alive = true;
    (async () => {
      setLoading(true);
      try {
        const res = await fetch(`/api/chats/${chatId}/messages?limit=${pageSize}`, {
          cache: 'no-store',
        });
        if (!res.ok) return;
        const data: PageResponse = normalizePage<MessageVM>(await res.json());
        if (!alive) return;

        const prepared = (data.items ?? []).map((m) => ({
          ...m,
          _mine: meId ? m.senderId === meId : undefined,
        }));
        const merged = mergeUniqueById([], prepared, (m) => m.id);
        setMessages(sortAsc(merged));
        setHasMore(!!data.hasMore);
        setNextBefore(data.nextBefore ?? null);

        setTimeout(scrollToBottom, 0);
      } finally {
        if (alive) setLoading(false);
      }
    })();
    return () => {
      alive = false;
    };
  }, [chatId, pageSize, meId, scrollToBottom]);

  const loadOlder = useCallback(async () => {
    if (!chatId || !hasMore || loading || !nextBefore) return;
    const el = containerRef.current;
    const prevHeight = el?.scrollHeight ?? 0;

    setLoading(true);
    try {
      const url = `/api/chats/${chatId}/messages?limit=${pageSize}&before=${encodeURIComponent(nextBefore)}`;
      const res = await fetch(url, { cache: 'no-store' });
      if (!res.ok) return;
      const data: PageResponse = normalizePage<MessageVM>(await res.json());

      const incoming = (data.items ?? []).map((m) => ({
        ...m,
        _mine: meId ? m.senderId === meId : undefined,
      }));
      setMessages((prev) => sortAsc(mergeUniqueById(incoming, prev, (m) => m.id)));

      setHasMore(!!data.hasMore);
      setNextBefore(data.nextBefore ?? null);

      setTimeout(() => {
        const newHeight = el?.scrollHeight ?? 0;
        if (el) el.scrollTop = (el.scrollTop ?? 0) + (newHeight - prevHeight);
      }, 0);
    } finally {
      setLoading(false);
    }
  }, [chatId, hasMore, loading, nextBefore, pageSize, meId]);

  const upsertMessage = useCallback((m: MessageVM) => {
    setMessages((prev) => {
      const merged = mergeUniqueById(prev, [m], (x) => x.id);
      return sortAsc(merged);
    });
  }, []);

  return {
    messages,
    setMessages,
    upsertMessage,
    hasMore,
    loading,
    loadOlder,
    containerRef,
    scrollToBottom,
    isNearBottom,
  };
}
