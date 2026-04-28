'use client';

import { useCallback } from 'react';
import { makeDmKey } from '@/lib/utils/makeDmKey';
import { byDateDesc, mergeUniqueById } from '@/lib/utils/pagination';
import type { Chat, DirectChatHeaderDto } from '@/types/chat';

const byLastActivityDesc = byDateDesc<Chat>((c) => c.lastMessage?.sentAt ?? c.createdAt);

async function getChatHeaderByChatId(chatId: string) {
  const resp = await fetch(`/api/chats/${encodeURIComponent(chatId)}/header`, {
    cache: 'no-store',
    credentials: 'include',
  });
  if (!resp.ok) throw new Error(await resp.text());
  return resp.json() as Promise<DirectChatHeaderDto>;
}

export function useDirectChatResolution(params: {
  chats: Chat[];
  setChats: React.Dispatch<React.SetStateAction<Chat[]>>;
  currentUserId: string | null;
}) {
  const { chats, setChats, currentUserId } = params;

  const openDirectChatWithUser = useCallback(
    async (userId: string): Promise<Chat> => {
      const meId = currentUserId;
      if (!meId) throw new Error('Current user id is not loaded yet');
      const dmKey = makeDmKey(meId, userId);

      const existing = chats.find((c) => c.id === dmKey || c.peerUserId === userId);
      if (existing) return existing;

      const headerResp = await fetch(`/api/chats/direct/${userId}/header`, {
        method: 'GET',
        credentials: 'include',
        cache: 'no-store',
      });
      const header = (await headerResp.json()) as DirectChatHeaderDto;

      const newChat: Chat = {
        id: dmKey,
        name: header.username ?? userId,
        isGroup: false,
        createdAt: new Date().toISOString(),
        lastMessage: undefined,
        unreadCount: 0,
        avatarUrl: header.avatarUrl ?? undefined,
        members: [],
        serverChatId: header.chatId ?? null,
        peerUserId: userId,
      };

      setChats((prev) => mergeUniqueById(prev, [newChat], (c) => c.id).sort(byLastActivityDesc));
      return newChat;
    },
    [chats, currentUserId, setChats]
  );

  const resolveChatSelection = useCallback(
    async (chatId: string): Promise<string | null> => {
      const card = chats.find((c) => c.id === chatId);
      if (!card) return null;
      if (card.isGroup) return card.id;

      if (card.peerUserId) {
        const resolved = await openDirectChatWithUser(card.peerUserId);
        return resolved.id;
      }

      const hdr = await getChatHeaderByChatId(chatId);
      const resolved = await openDirectChatWithUser(hdr.otherUserId);
      return resolved.id;
    },
    [chats, openDirectChatWithUser]
  );

  return {
    openDirectChatWithUser,
    resolveChatSelection,
  };
}
