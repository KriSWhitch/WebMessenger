'use client';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { byDateDesc, mergeUniqueById, normalizePage } from '@/lib/utils/pagination';

export type ChatMessagePreviewDto = {
  id: string;
  senderId: string;
  snippet: string;
  sentAt: string;
};

export type ChatListItemDto = {
  id: string;
  isGroup: boolean;
  title?: string | null;
  avatarUrl?: string | null;
  lastActivityAt: string;
  lastMessage?: ChatMessagePreviewDto;
  unreadCount: number;
  hasUnread: boolean;
};

type PageResponse = {
  items: ChatListItemDto[];
  hasMore: boolean;
  nextBefore?: string | null;
};

const byLastActivityDesc = byDateDesc<ChatListItemDto>((c) => c.lastActivityAt);

export function useChatList(opts: { pageSize?: number } = {}) {
  const { pageSize = 20 } = opts;

  const [pages, setPages] = useState<ChatListItemDto[][]>([]);
  const [hasMore, setHasMore] = useState(false);
  const [nextBefore, setNextBefore] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const flat = useMemo(() => pages.flat().sort(byLastActivityDesc), [pages]);

  useEffect(() => {
    let alive = true;
    (async () => {
      setLoading(true);
      try {
        const res = await fetch(`/api/chats?limit=${pageSize}`, {
          cache: 'no-store',
          credentials: 'include',
        });
        if (!res.ok) return;
        const data: PageResponse = normalizePage<ChatListItemDto>(await res.json());
        if (!alive) return;
        setPages(data.items.length ? [data.items] : [[]]);
        setHasMore(!!data.hasMore);
        setNextBefore(data.nextBefore ?? null);
      } finally {
        if (alive) setLoading(false);
      }
    })();
    return () => {
      alive = false;
    };
  }, [pageSize]);

  const loadMore = useCallback(async () => {
    if (!hasMore || loading || !nextBefore) return;
    setLoading(true);
    try {
      const url = `/api/chats?limit=${pageSize}&before=${encodeURIComponent(nextBefore)}`;
      const res = await fetch(url, { cache: 'no-store', credentials: 'include' });
      if (!res.ok) return;
      const data: PageResponse = normalizePage<ChatListItemDto>(await res.json());
      setPages((prev) => [...prev, data.items ?? []]);
      setHasMore(!!data.hasMore);
      setNextBefore(data.nextBefore ?? null);
    } finally {
      setLoading(false);
    }
  }, [hasMore, loading, nextBefore, pageSize]);

  const upsertFromMessage = useCallback(
    (payload: {
      chatId: string;
      message: { id: string; senderId: string; content: string; sentAt: string };
      meUserId?: string | null;
    }) => {
      const nowCard: ChatListItemDto = {
        id: payload.chatId,
        isGroup: false,
        title: null,
        avatarUrl: null,
        lastActivityAt: payload.message.sentAt,
        lastMessage: {
          id: payload.message.id,
          senderId: payload.message.senderId,
          snippet:
            payload.message.content.length > 120
              ? payload.message.content.slice(0, 120) + '…'
              : payload.message.content,
          sentAt: payload.message.sentAt,
        },
        unreadCount: payload.meUserId && payload.message.senderId !== payload.meUserId ? 1 : 0,
        hasUnread: !!(payload.meUserId && payload.message.senderId !== payload.meUserId),
      };

      setPages((prev) => {
        const merged = mergeUniqueById(prev.flat(), [nowCard], (c) => c.id).sort(byLastActivityDesc);
        const newPages: ChatListItemDto[][] = [];
        for (let i = 0; i < merged.length; i += pageSize)
          newPages.push(merged.slice(i, i + pageSize));
        return newPages.length ? newPages : [[]];
      });
    },
    [pageSize]
  );

  return { chats: flat, hasMore, loading, loadMore, upsertFromMessage };
}
