'use client';

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import clsx from "clsx";
import { MessengerMainArea } from "@/components/features/messenger/layout/MessengerMainArea";
import { MessengerSidebar } from "@/components/features/messenger/layout/MessengerSidebar";
import { UserSettings } from '@/components/features/messenger/UserSettings/UserSettings';
import { UserProfilePanel } from "@/components/features/messenger/chat/UserProfilePanel";
import * as signalR from '@microsoft/signalr';
import { getChatConnection } from '@/lib/hubs/chatHubClient';

import type {
  Chat,
  DirectChatHeaderDto,
  ChatListItemDto,
  PagedResult,
  MessageCreatedPayload,
} from '@/types/chat';

function normalizePage(json: any): PagedResult<ChatListItemDto> {
  if (json && Array.isArray(json.items)) {
    return { items: json.items, hasMore: !!json.hasMore, nextBefore: json.nextBefore ?? null };
  }
  const items = Array.isArray(json?.data) ? json.data : [];
  return { items, hasMore: !!json?.hasMore, nextBefore: json?.nextBefore ?? null };
}

function byLastActivityDesc(a: Chat, b: Chat) {
  const aTs = new Date(a.lastMessage?.sentAt ?? a.createdAt).getTime();
  const bTs = new Date(b.lastMessage?.sentAt ?? b.createdAt).getTime();
  return bTs - aTs;
}

function mergeUniqueById(base: Chat[], incoming: Chat[]) {
  const map = new Map<string, Chat>();
  for (const c of base) map.set(c.id, c);
  for (const c of incoming) map.set(c.id, c);
  return Array.from(map.values());
}

function dtoToChat(dto: ChatListItemDto): Chat {
  const isDirect = !dto.isGroup;
  return {
    id: dto.id,
    serverChatId: dto.id,
    isGroup: dto.isGroup,
    name: isDirect ? (dto.peerUsername ?? dto.title ?? 'Direct chat') : (dto.title ?? 'Group'),
    createdAt: dto.lastActivityAt,
    avatarUrl: isDirect ? (dto.peerAvatarUrl ?? dto.avatarUrl ?? undefined)
                        : (dto.avatarUrl ?? undefined),
    lastMessage: dto.lastMessage
      ? {
          id: dto.lastMessage.id,
          content: dto.lastMessage.snippet ?? '',
          senderId: dto.lastMessage.senderId,
          chatId: dto.id,
          sentAt: dto.lastMessage.sentAt,
          isRead: false,
        }
      : undefined,
    unreadCount: dto.unreadCount ?? 0,
    members: [],
    peerUserId: isDirect ? dto.peerUserId?.toString() : undefined, // приведи Guid -> string, если у вас тип string
  };
}

export default function MessengerPage() {
  const [chats, setChats] = useState<Chat[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [isSearchFocused, setIsSearchFocused] = useState(false);
  const [showSettings, setShowSettings] = useState(false);
  const [isProfileOpen, setIsProfileOpen] = useState(false);
  const [selectedChat, setSelectedChat] = useState<string | null>(null);
  const [profileUserId, setProfileUserId] = useState<string | null>(null);

  const [hasMore, setHasMore] = useState(false);
  const [nextBefore, setNextBefore] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const currentUserIdRef = useRef<string | null>(null);

  useEffect(() => {
    let alive = true;
    (async () => {
      setLoading(true);
      try {
        const res = await fetch(`/api/chats?limit=20`, { cache: 'no-store', credentials: 'include' });
        if (!res.ok) return;
        const data = normalizePage(await res.json());
        if (!alive) return;

        const mapped = (data.items ?? []).map(dtoToChat);
        setChats(mapped.sort(byLastActivityDesc));
        setHasMore(!!data.hasMore);
        setNextBefore(data.nextBefore ?? null);
      } finally {
        if (alive) setLoading(false);
      }
    })();
    return () => { alive = false; };
  }, []);

  const loadMoreChats = useCallback(async () => {
    if (!hasMore || loading || !nextBefore) return;
    setLoading(true);
    try {
      const url = `/api/chats?limit=20&before=${encodeURIComponent(nextBefore)}`;
      const res = await fetch(url, { cache: 'no-store', credentials: 'include' });
      if (!res.ok) return;
      const data = normalizePage(await res.json());
      const mapped = (data.items ?? []).map(dtoToChat);
      setChats(prev => mergeUniqueById(prev, mapped).sort(byLastActivityDesc));
      setHasMore(!!data.hasMore);
      setNextBefore(data.nextBefore ?? null);
    } finally {
      setLoading(false);
    }
  }, [hasMore, loading, nextBefore]);

  useEffect(() => {
    const conn = getChatConnection();

    const start = async () => {
      if (conn.state === signalR.HubConnectionState.Disconnected) {
        try { await conn.start(); } catch (e) { console.error('Hub start failed:', e); }
      }
    };

  const onMessageCreated = (payload: MessageCreatedPayload) => {
    const mine = currentUserIdRef.current && payload.message.senderId === currentUserIdRef.current;

    setChats(prev => {
      const existing = prev.find(c => c.id === payload.chatId);
      const snippet = payload.message.content.length > 120
        ? payload.message.content.slice(0, 120) + '…'
        : payload.message.content;

      const preview = {
        id: payload.message.id,
        content: snippet,
        senderId: payload.message.senderId,
        chatId: payload.chatId,
        sentAt: payload.message.sentAt,
        isRead: !!payload.message.isRead && !!mine,
      };

      if (existing) {
        const updated: Chat = {
          ...existing,
          lastMessage: preview,
          unreadCount: mine ? existing.unreadCount : (existing.unreadCount ?? 0) + 1,
          // если пришёл peerUserId, обновим и его
          peerUserId: existing.peerUserId ?? payload.peerUserId ?? existing.peerUserId,
        };
        return prev.map(c => c.id === existing.id ? updated : c).sort(byLastActivityDesc);
      }

      const stub: Chat = {
        id: payload.chatId,
        serverChatId: payload.chatId,
        isGroup: false,
        name: 'Direct chat',
        createdAt: payload.message.sentAt,
        avatarUrl: undefined,
        lastMessage: preview,
        unreadCount: mine ? 0 : 1,
        members: [],
        // КЛЮЧЕВОЕ: подставляем peerUserId
        peerUserId: payload.peerUserId
          ?? (!mine ? payload.message.senderId : undefined),
      };
      return [stub, ...prev].sort(byLastActivityDesc);
    });
  };

    conn.on('MessageCreated', onMessageCreated);
    void start();

    const onReconnected = () => { /* ничего — user:{me} уже подписан на сервере */ };
    conn.onreconnected(onReconnected);

    return () => {
      conn.off('MessageCreated', onMessageCreated);
      conn.onreconnected(() => {});
    };
  }, []);

  const openDirectChatWithUser = useCallback(async (userId: string): Promise<Chat> => {
    const existing = chats.find(c => c.peerUserId === userId || c.id === `dm-${userId}`);
    if (existing) return existing;

    const headerResp = await fetch(`/api/chats/direct/${userId}/header`, {
      method: 'GET',
      cache: 'no-store',
      credentials: 'include',
    });
    if (!headerResp.ok) {
      throw new Error(await headerResp.text());
    }
    const header = (await headerResp.json()) as DirectChatHeaderDto;

    const newChat: Chat = {
      id: `dm-${userId}`,
      name: header.username ?? userId,
      isGroup: false,
      createdAt: new Date().toISOString(),
      lastMessage: undefined,
      unreadCount: 0,
      avatarUrl: header.avatarUrl ?? undefined,
      members: [{ userId }, { userId: 'current-user' }],
      serverChatId: header.chatId ?? null,
      peerUserId: userId,
    };

    setChats(prev => {
      if (header.chatId && prev.some(c => c.id === header.chatId)) return prev;
      return mergeUniqueById(prev, [newChat]).sort(byLastActivityDesc);
    });

    return newChat;
  }, [chats]);

  const handleUserSelect = useCallback(async (userId: string) => {
    try {
      const chat = await openDirectChatWithUser(userId);
      setSelectedChat(chat.id);
      setSearchQuery('');
      setShowSettings(false);
    } catch (e) {
      console.error('Failed to open direct chat:', e);
    }
  }, [openDirectChatWithUser]);

  
const handleChatSelect = useCallback(async (chatId: string) => {
  const card = chats.find(c => c.id === chatId);
  if (!card) return;

  if (card.isGroup) {
    setSelectedChat(card.id);
    setShowSettings(false);
    return;
  }
  
  if (card.peerUserId) {
    const resolved = await openDirectChatWithUser(card.peerUserId);
    setSelectedChat(resolved.id);
    setShowSettings(false);
    return;
  }
  
  try {
    const hdr = await getChatHeaderByChatId(chatId);
    const resolved = await openDirectChatWithUser(hdr.otherUserId);
    setSelectedChat(resolved.id);
    setShowSettings(false);
  } catch (e) {
    console.error('Failed to resolve header by chatId:', e);
    setSelectedChat(card.id);
    setShowSettings(false);
  }
}, [chats, openDirectChatWithUser]);


  async function getChatHeaderByChatId(chatId: string) {
    const resp = await fetch(`/api/chats/${encodeURIComponent(chatId)}/header`, {
      cache: 'no-store',
      credentials: 'include',
    });
    if (!resp.ok) {
      throw new Error(await resp.text());
    }
    return resp.json() as Promise<DirectChatHeaderDto>;
  }

  const handleAddContact = useCallback(async (userId: string) => {
    try {
      const resp = await fetch(`/api/contacts/add`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ contactUserId: userId })
      });
      if (!resp.ok) {
        console.error('Failed to add contact:', await resp.text());
      }
    } catch (error) {
      console.error('Failed to add contact:', error);
    }
  }, []);

  const selectedChatObj = useMemo(
    () => chats.find(chat => chat.id === selectedChat) ?? undefined,
    [chats, selectedChat]
  );

  const openProfilePanel = useCallback((userId: string) => {
    setProfileUserId(userId);
    setIsProfileOpen(true);
  }, []);

  const closeProfilePanel = useCallback(() => {
    setIsProfileOpen(false);
  }, []);

  const handleCloseChat = useCallback(() => {
    setIsProfileOpen(false);
    setShowSettings(false);
    setSelectedChat(null);
  }, []);

  return (
    <div className="relative flex h-screen bg-gray-900 text-gray-200 overflow-hidden">
      <div
        className={clsx(
          "relative flex-shrink-0 border-r border-gray-700 z-[20]",
          (selectedChat && !showSettings) ? "w-0" : "w-full",
          "md:w-80 lg:w-96",
          "transition-[width] duration-0"
        )}
      >
        <div
          className={clsx(
            "h-full flex flex-col",
            selectedChat ? "hidden md:flex" : "flex"
          )}
        >
          <MessengerSidebar
            searchQuery={searchQuery}
            setSearchQuery={setSearchQuery}
            isSearchFocused={isSearchFocused}
            setIsSearchFocused={setIsSearchFocused}
            chats={chats}
            onSelectChat={handleChatSelect}
            selectedChatId={selectedChat}
            onSearchUserSelect={handleUserSelect}
            onAddContact={handleAddContact}
            contacts={[]}
            selectedContactId={null}
            onSelectContact={handleUserSelect}
            onSettingsClick={() => setShowSettings(true)}
          />

          {hasMore && (
            <div className="p-3 border-t border-gray-800">
              <button
                className="w-full rounded bg-gray-800 px-3 py-2 text-sm hover:bg-gray-700 disabled:opacity-60"
                disabled={loading}
                onClick={loadMoreChats}
              >
                {loading ? 'Loading…' : 'Show more'}
              </button>
            </div>
          )}
        </div>

        <aside
          className={clsx(
            "absolute inset-0 z-[40]",
            "bg-gray-900 border-r border-gray-700",
            "will-change-transform transform transition-transform duration-300 ease-out",
            "transition-opacity duration-300",
            showSettings
              ? "translate-x-0 opacity-100 pointer-events-auto"
              : "-translate-x-full opacity-0 pointer-events-none"
          )}
        >
          <UserSettings onClose={() => setShowSettings(false)} />
        </aside>
      </div>

      <MessengerMainArea
        hasChats={chats.length > 0}
        selectedChat={selectedChatObj}
        onOpenProfile={openProfilePanel}
        onCloseChat={handleCloseChat}
      />

      {profileUserId && (
        <UserProfilePanel
          userId={profileUserId}
          open={isProfileOpen}
          onClose={closeProfilePanel}
        />
      )}
    </div>
  );
}