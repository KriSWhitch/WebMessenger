import type { Chat } from '@/types/chat';
import type { ChatListItemDto } from '@/hooks/useChatList';

export function dtoToChat(dto: ChatListItemDto): Chat {
  const isDirect = !dto.isGroup;
  return {
    id: dto.id,
    serverChatId: dto.id,
    isGroup: dto.isGroup,
    name: isDirect
      ? ((dto as { peerUsername?: string | null }).peerUsername ?? dto.title ?? 'Direct chat')
      : (dto.title ?? 'Group'),
    createdAt: dto.lastActivityAt,
    avatarUrl: isDirect
      ? ((dto as { peerAvatarUrl?: string | null }).peerAvatarUrl ?? dto.avatarUrl ?? undefined)
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
    peerUserId: isDirect ? (dto as { peerUserId?: string | null }).peerUserId ?? undefined : undefined,
  };
}
