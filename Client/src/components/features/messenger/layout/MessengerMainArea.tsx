'use client';
import React, { useMemo, useState } from 'react';
import { Chat } from '@/types/chat';
import { EmptyState } from '../EmptyState/EmptyState';
import { ChatIconSecondary } from '@/components/icons/ChatIconSecondary';
import { ChatHeader } from '../chat/ChatHeader';
import { useChatRealtime } from '@/hooks/useChatRealtime';
import type { ReadReceipt } from '@/hooks/useChatRealtime';
import { useMessages } from '@/hooks/useMessages';
import { MessageList } from '@/components/features/messenger/chat/MessageList';
import type { MessageVM } from '@/components/features/messenger/chat/MessageBubble';
import { MessageComposer } from '@/components/features/messenger/chat/MessageComposer';
import { useCurrentUser } from '@/hooks/useCurrentUser';
import { useReadStateTracking } from '@/hooks/useReadStateTracking';

interface MessengerMainAreaProps {
  hasChats: boolean;
  selectedChat?: Chat;
  onOpenProfile?: (userId: string) => void;
  onCloseChat?: () => void;
  onChatRead?: (chatId: string, lastReadAt: string, unreadCount: number) => void;
}

function getServerChatId(c?: Chat | undefined): string | null {
  if (!c) return null;
  return (c as Chat).serverChatId ?? null;
}

export const MessengerMainArea = ({
  hasChats,
  selectedChat,
  onOpenProfile,
  onCloseChat,
  onChatRead,
}: MessengerMainAreaProps) => {
  const { currentUserId: meId } = useCurrentUser();
  const chatViewOpen = !!selectedChat;
  const [chatId, setChatId] = useState<string | null>(getServerChatId(selectedChat));

  const peerUserId = useMemo(() => {
    if (!selectedChat) return '';
    if (selectedChat.peerUserId) return selectedChat.peerUserId;

    if (meId && selectedChat.members?.length) {
      const other = selectedChat.members.find((m) => m.userId !== meId);
      if (other) return other.userId;
    }

    return '';
  }, [selectedChat, meId]);

  const {
    messages,
    setMessages,
    upsertMessage,
    hasMore,
    loading,
    loadOlder,
    containerRef,
    scrollToBottom,
    isNearBottom,
  } = useMessages({ chatId, pageSize: 30, meId });

  React.useEffect(() => {
    setChatId(getServerChatId(selectedChat));
  }, [selectedChat, setMessages]);

  const { scheduleReadIfNeeded } = useReadStateTracking({
    chatViewOpen,
    chatId,
    messages,
    meId,
    isNearBottom,
    setMessages,
    onChatRead,
  });

  useChatRealtime({
    chatId: chatViewOpen ? (chatId ?? undefined) : undefined,
    peerUserId: chatViewOpen && !chatId ? peerUserId || undefined : undefined,
    currentUserId: meId || undefined,
    onMessage: React.useCallback(
      (evt) => {
        if (!chatViewOpen) return;

        const srv: MessageVM = {
          ...evt.message,
          _mine: meId ? evt.message.senderId === meId : undefined,
        };
        upsertMessage(srv);

        if (!chatId && evt.chatId) setChatId(evt.chatId);

        const isMine = meId ? evt.message.senderId === meId : false;
        if (!isMine) scheduleReadIfNeeded();

        if (isNearBottom()) setTimeout(scrollToBottom, 0);
      },
      [
        chatViewOpen,
        chatId,
        meId,
        upsertMessage,
        isNearBottom,
        scrollToBottom,
        scheduleReadIfNeeded,
      ]
    ),

    onReadReceipt: React.useCallback(
      (p: ReadReceipt) => {
        if (!chatViewOpen) return;
        if (p.chatId && p.chatId === chatId && p.userId === meId) {
          onChatRead?.(p.chatId, p.lastReadAt, 0);
        }
      },
      [chatViewOpen, chatId, meId, onChatRead]
    ),
  });

  const header = useMemo(() => {
    if (!selectedChat) return null;
    return (
      <ChatHeader
        peerUserId={peerUserId}
        username={selectedChat.name}
        avatarUrl={selectedChat.avatarUrl}
        onOpenProfile={() => onOpenProfile?.(peerUserId)}
        onBack={onCloseChat}
        showBackButton={true}
      />
    );
  }, [selectedChat, peerUserId, onOpenProfile, onCloseChat]);

  if (!selectedChat) {
    return (
      <div className="flex-1 flex flex-col relative z-[30]">
        <div className="flex-1 overflow-y-auto bg-gray-800/50">
          <EmptyState
            icon={<ChatIconSecondary />}
            title={hasChats ? 'Select a chat' : 'Welcome to your messenger'}
            description={
              hasChats
                ? 'Choose a conversation from the list to start messaging'
                : 'Get started by creating your first chat'
            }
          />
        </div>
      </div>
    );
  }

  return (
    <div className="flex-1 flex flex-col relative z-[30]">
      {header}
      <div className="flex-1 overflow-y-auto bg-gray-800/50">
        <MessageList
          messages={messages}
          externalContainerRef={containerRef}
          hasMore={hasMore}
          loading={loading}
          onLoadMore={loadOlder}
        />
      </div>

      <div className="p-3 md:p-4 border-t border-gray-700 bg-gray-900/60 backdrop-blur">
        <div className="bg-gray-900 rounded-2xl px-2 py-2">
          <MessageComposer
            chatId={chatId}
            meId={meId}
            peerUserId={peerUserId}
            setMessages={setMessages}
            isNearBottom={isNearBottom}
            scrollToBottom={scrollToBottom}
            onChatIdResolved={setChatId}
          />
        </div>
      </div>
    </div>
  );
};
