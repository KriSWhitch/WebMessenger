'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { getChatConnection } from '@/lib/hubs/chatHubClient';
import { makeDmKey } from '@/lib/utils/makeDmKey';
import { byDateDesc, mergeUniqueById, normalizePage } from '@/lib/utils/pagination';
import { dtoToChat } from '@/lib/utils/normalization';
import type { Chat, ChatListItemDto, MessageCreatedPayload, PagedResult } from '@/types/chat';

const byLastActivityDesc = byDateDesc<Chat>((c) => c.lastMessage?.sentAt ?? c.createdAt);

async function getChatHeaderByChatId(chatId: string) {
  const resp = await fetch(`/api/chats/${encodeURIComponent(chatId)}/header`, {
    cache: 'no-store',
    credentials: 'include',
  });
  if (!resp.ok) throw new Error(await resp.text());
  return resp.json() as Promise<{ username?: string | null; avatarUrl?: string | null; otherUserId: string }>;
}

export function useChatListManagement(params: {
  currentUserId: string | null;
  selectedServerChatId: string | null;
  selectedPeerUserId: string | null;
  setSelectedChat: React.Dispatch<React.SetStateAction<string | null>>;
}) {
  const { currentUserId, selectedServerChatId, selectedPeerUserId, setSelectedChat } = params;

  const [chats, setChats] = useState<Chat[]>([]);
  const [hasMore, setHasMore] = useState(false);
  const [nextBefore, setNextBefore] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const currentUserIdRef = useRef<string | null>(currentUserId);
  const selectedServerChatIdRef = useRef<string | null>(selectedServerChatId);
  const selectedPeerUserIdRef = useRef<string | null>(selectedPeerUserId);

  useEffect(() => {
    currentUserIdRef.current = currentUserId;
  }, [currentUserId]);

  useEffect(() => {
    selectedServerChatIdRef.current = selectedServerChatId;
  }, [selectedServerChatId]);

  useEffect(() => {
    selectedPeerUserIdRef.current = selectedPeerUserId;
  }, [selectedPeerUserId]);

  useEffect(() => {
    let alive = true;
    (async () => {
      setLoading(true);
      try {
        const res = await fetch('/api/chats?limit=20', {
          cache: 'no-store',
          credentials: 'include',
        });
        if (!res.ok) return;
        const data: PagedResult<ChatListItemDto> = normalizePage<ChatListItemDto>(await res.json());
        if (!alive) return;
        const mapped = (data.items ?? []).map(dtoToChat);
        setChats(mapped.sort(byLastActivityDesc));
        setHasMore(!!data.hasMore);
        setNextBefore(data.nextBefore ?? null);
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => {
      alive = false;
    };
  }, []);

  const loadMoreChats = useCallback(async () => {
    if (!hasMore || loading || !nextBefore) return;
    setLoading(true);
    try {
      const url = `/api/chats?limit=20&before=${encodeURIComponent(nextBefore)}`;
      const res = await fetch(url, { cache: 'no-store', credentials: 'include' });
      if (!res.ok) return;
      const data: PagedResult<ChatListItemDto> = normalizePage<ChatListItemDto>(await res.json());
      const mapped = (data.items ?? []).map(dtoToChat);
      setChats((prev) => mergeUniqueById(prev, mapped, (c) => c.id).sort(byLastActivityDesc));
      setHasMore(!!data.hasMore);
      setNextBefore(data.nextBefore ?? null);
    } finally {
      setLoading(false);
    }
  }, [hasMore, loading, nextBefore]);

  const seenIdsRef = useRef<string[]>([]);
  const seenSetRef = useRef<Set<string>>(new Set());

  const rememberId = useCallback((id: string) => {
    if (seenSetRef.current.has(id)) return true;
    seenSetRef.current.add(id);
    seenIdsRef.current.push(id);
    if (seenIdsRef.current.length > 500) {
      const drop = seenIdsRef.current.splice(0, seenIdsRef.current.length - 500);
      drop.forEach((d) => seenSetRef.current.delete(d));
    }
    return false;
  }, []);

  useEffect(() => {
    const conn = getChatConnection();

    const start = async () => {
      if (conn.state === signalR.HubConnectionState.Disconnected) {
        try {
          await conn.start();
        } catch (e) {
          console.error('Hub start failed:', e);
        }
      }
    };

    const onMessageCreated = (payload: MessageCreatedPayload & { peerUserId?: string }) => {
      if (rememberId(payload.message.id)) return;
      const meId = currentUserIdRef.current;
      const mine = !!meId && payload.message.senderId === meId;

      const activeServerId = selectedServerChatIdRef.current;
      const activePeerId = selectedPeerUserIdRef.current;
      const isActive =
        (payload.chatId && payload.chatId === activeServerId) ||
        (!!activePeerId &&
          (payload.peerUserId === activePeerId || payload.message.senderId === activePeerId));

      const shouldIncrement = !mine && !isActive;

      setChats((prev) => {
        if (payload.peerUserId && meId) {
          const dmKey = makeDmKey(meId, payload.peerUserId);
          const stubIdx = prev.findIndex((c) => c.id === dmKey && !c.serverChatId);
          if (stubIdx >= 0) {
            const stub = prev[stubIdx];
            const upgraded: Chat = {
              ...stub,
              id: payload.chatId,
              serverChatId: payload.chatId,
              lastMessage: {
                id: payload.message.id,
                content:
                  payload.message.content.length > 120
                    ? payload.message.content.slice(0, 120) + '…'
                    : payload.message.content,
                senderId: payload.message.senderId,
                chatId: payload.chatId,
                sentAt: payload.message.sentAt,
                isRead: mine,
              },
              unreadCount: shouldIncrement ? (stub.unreadCount ?? 0) + 1 : (stub.unreadCount ?? 0),
            };
            const next = [...prev];
            next[stubIdx] = upgraded;

            setSelectedChat((sel) => (sel === dmKey ? payload.chatId : sel));
            return next.sort(byLastActivityDesc);
          }
        }

        const existingIdx = prev.findIndex((c) => c.id === payload.chatId);
        if (existingIdx >= 0) {
          const existing = prev[existingIdx];
          const updated: Chat = {
            ...existing,
            lastMessage: {
              id: payload.message.id,
              content:
                payload.message.content.length > 120
                  ? payload.message.content.slice(0, 120) + '…'
                  : payload.message.content,
              senderId: payload.message.senderId,
              chatId: payload.chatId,
              sentAt: payload.message.sentAt,
              isRead: mine,
            },
            unreadCount: shouldIncrement ? (existing.unreadCount ?? 0) + 1 : (existing.unreadCount ?? 0),
            peerUserId: existing.peerUserId ?? payload.peerUserId ?? existing.peerUserId,
          };
          const next = [...prev];
          next[existingIdx] = updated;
          return next.sort(byLastActivityDesc);
        }

        const newChat: Chat = {
          id: payload.chatId,
          serverChatId: payload.chatId,
          isGroup: false,
          name: 'Direct chat',
          createdAt: payload.message.sentAt,
          avatarUrl: undefined,
          lastMessage: {
            id: payload.message.id,
            content:
              payload.message.content.length > 120
                ? payload.message.content.slice(0, 120) + '…'
                : payload.message.content,
            senderId: payload.message.senderId,
            chatId: payload.chatId,
            sentAt: payload.message.sentAt,
            isRead: mine,
          },
          unreadCount: shouldIncrement ? 1 : 0,
          members: [],
          peerUserId: payload.peerUserId ?? (!mine ? payload.message.senderId : undefined),
        };

        const next = [newChat, ...prev];
        setTimeout(async () => {
          try {
            const hdr = await getChatHeaderByChatId(payload.chatId);
            setChats((prevChats) => {
              const idx = prevChats.findIndex((c) => c.id === payload.chatId);
              if (idx < 0) return prevChats;
              const updated = {
                ...prevChats[idx],
                name: hdr.username ?? 'Direct chat',
                avatarUrl: hdr.avatarUrl ?? undefined,
                peerUserId: hdr.otherUserId ?? prevChats[idx].peerUserId,
              };
              const copy = [...prevChats];
              copy[idx] = updated;
              return copy;
            });
          } catch (e) {
            console.error('Failed to fetch chat header:', e);
          }
        }, 0);

        return next.sort(byLastActivityDesc);
      });
    };

    const onReadReceipt = (p: { chatId: string; userId: string; lastReadAt: string }) => {
      const meId = currentUserIdRef.current;
      if (!meId || p.userId !== meId) return;
      setChats((prev) => prev.map((c) => (c.id === p.chatId ? { ...c, unreadCount: 0 } : c)));
    };

    conn.off('MessageCreated', onMessageCreated);
    conn.on('MessageCreated', onMessageCreated);
    conn.off('ReadReceipt', onReadReceipt);
    conn.on('ReadReceipt', onReadReceipt);
    void start();

    conn.onreconnected(() => {});

    return () => {
      conn.off('MessageCreated', onMessageCreated);
      conn.off('ReadReceipt', onReadReceipt);
      conn.onreconnected(() => {});
    };
  }, [rememberId, setSelectedChat]);

  const onChatRead = useCallback((serverChatId: string) => {
    setChats((prev) => prev.map((c) => (c.id === serverChatId ? { ...c, unreadCount: 0 } : c)));
  }, []);

  return {
    chats,
    setChats,
    hasMore,
    loading,
    loadMoreChats,
    onChatRead,
  };
}
