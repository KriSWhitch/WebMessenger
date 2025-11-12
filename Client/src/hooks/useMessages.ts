'use client';
import { useCallback, useEffect, useRef, useState } from 'react';
import type { MessageVM } from '@/components/features/messenger/chat/MessageBubble';

type PageResponse = {
  items: MessageVM[];
  hasMore: boolean;
  nextBefore?: string | null;
};

function normalizePage(json: any): PageResponse {
  if (json && Array.isArray(json.items)) {
    return { items: json.items, hasMore: !!json.hasMore, nextBefore: json.nextBefore ?? null };
  }
  const items = Array.isArray(json?.data) ? json.data : [];
  return { items, hasMore: !!json?.hasMore, nextBefore: json?.nextBefore ?? null };
}

function sortAsc(arr: MessageVM[]) {
  return [...arr].sort((a, b) => a.sentAt.localeCompare(b.sentAt));
}

function mergeUniqueById(base: MessageVM[], incoming: MessageVM[]) {
  const map = new Map<string, MessageVM>();
  for (const m of base) map.set(m.id, m);
  for (const m of incoming) map.set(m.id, m);
  return Array.from(map.values());
}

export function useMessages(opts: { chatId: string | null; pageSize?: number; meId?: string | null; }) {
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
        const res = await fetch(`/api/chats/${chatId}/messages?limit=${pageSize}`, { cache: 'no-store' });
        if (!res.ok) return;
        const data = normalizePage(await res.json());
        if (!alive) return;

        const prepared = (data.items ?? []).map(m => ({ ...m, _mine: meId ? m.senderId === meId : undefined }));
        const merged = mergeUniqueById([], prepared);
        setMessages(sortAsc(merged));
        setHasMore(!!data.hasMore);
        setNextBefore(data.nextBefore ?? null);

        setTimeout(scrollToBottom, 0);
      } finally {
        if (alive) setLoading(false);
      }
    })();
    return () => { alive = false; };
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
      const data = normalizePage(await res.json());

      const incoming = (data.items ?? []).map(m => ({ ...m, _mine: meId ? m.senderId === meId : undefined }));
      setMessages(prev => sortAsc(mergeUniqueById(incoming, prev)));

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
    setMessages(prev => {
      const merged = mergeUniqueById(prev, [m]);
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