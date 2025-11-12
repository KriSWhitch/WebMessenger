'use client';

import React, { useCallback, useMemo, useState } from 'react';

import { Chat } from '@/types/chat';
import { EmptyState } from '../EmptyState/EmptyState';
import { ChatIconSecondary } from '@/components/icons/ChatIconSecondary';
import { ChatHeader } from '../chat/ChatHeader';

import { InputField } from '@/components/ui/Input/Input';
import { Button } from '@/components/ui/Button/Button';
import { useChatRealtime } from '@/hooks/useChatRealtime';
import { useMessages } from '@/hooks/useMessages';
import { MessageList } from '@/components/features/messenger/chat/MessageList';
import type { MessageVM } from '@/components/features/messenger/chat/MessageBubble';
import { SendMessageIcon } from '@/components/icons/SendMessageIcon';

interface MessengerMainAreaProps {
  hasChats: boolean;
  selectedChat?: Chat;
  onOpenProfile?: (userId: string) => void;
  onCloseChat?: () => void;
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
}: MessengerMainAreaProps) => {
  const peerUserId = useMemo(() => {
    if (!selectedChat) return '';
    return (
      (selectedChat as Chat).peerUserId ??
      selectedChat.members?.find(m => m.userId !== 'current-user')?.userId ??
      ''
    );
  }, [selectedChat]);

  const [chatId, setChatId] = useState<string | null>(getServerChatId(selectedChat));

  const [meId, setMeId] = useState<string | null>(null);
  React.useEffect(() => {
    let alive = true;
    (async () => {
      try {
        const r = await fetch('/api/users/profile', { cache: 'no-store' });
        if (!r.ok) return;
        const profile = await r.json();
        if (alive) setMeId(profile?.id ?? null);
      } catch {/* noop */}
    })();
    return () => { alive = false; };
  }, []);

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

  useChatRealtime({
    chatId: chatId ?? undefined,
    peerUserId: chatId ? undefined : (peerUserId || undefined),
    onMessage: React.useCallback((evt: any) => {
      const srv: MessageVM = {
        ...evt.message,
        _mine: meId ? evt.message.senderId === meId : undefined,
      };
      upsertMessage(srv);

      if (!chatId && evt.chatId) setChatId(evt.chatId);
      if (isNearBottom()) setTimeout(scrollToBottom, 0);
    }, [chatId, meId, upsertMessage, isNearBottom, scrollToBottom]),
  });

  const [text, setText] = useState('');
  const [isSending, setIsSending] = useState(false);
  const canSend = text.trim().length > 0 && !isSending && !!peerUserId;

const onSend = useCallback(async () => {
  if (!canSend) return;
  setIsSending(true);

  const clientId = `client-${crypto.randomUUID()}`;
  const optimistic: MessageVM = {
    id: clientId,
    chatId: chatId ?? 'pending',
    senderId: meId ?? 'me',
    content: text.trim(),
    sentAt: new Date().toISOString(),
    isRead: false,
    _pending: true,
    _mine: true,
  };

  setMessages(prev => [...prev, optimistic]);
  setText('');
  setTimeout(() => { if (isNearBottom()) scrollToBottom(); }, 0);

  try {
    const res = await fetch(`/api/chats/direct/${peerUserId}/messages`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ content: optimistic.content }),
    });

    if (!res.ok) {
      setMessages(prev => prev.map(m => m.id === clientId ? { ...m, _pending: false, _failed: true } : m));
      return;
    }

    const data = await res.json() as { chatId: string; message: MessageVM };

    setMessages(prev => prev.filter(m => m.id !== clientId));

    if (!chatId) setChatId(data.chatId);
    setTimeout(() => { if (isNearBottom()) scrollToBottom(); }, 0);

  } catch {
    setMessages(prev => prev.map(m => m.id === clientId ? { ...m, _pending: false, _failed: true } : m));
  } finally {
    setIsSending(false);
  }
}, [canSend, chatId, meId, peerUserId, text, setMessages, isNearBottom, scrollToBottom]);

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

  return (
    <div className="flex-1 flex flex-col relative z-[30]">
      {!selectedChat ? (
        <>
          <div className="flex-1 overflow-y-auto bg-gray-800/50">
            <EmptyState
              icon={<ChatIconSecondary />}
              title={hasChats ? "Select a chat" : "Welcome to your messenger"}
              description={
                hasChats
                  ? "Choose a conversation from the list to start messaging"
                  : "Get started by creating your first chat"
              }
            />
          </div>
        </>
      ) : (
        <>
          {header}

          <div className="flex-1 overflow-y-auto bg-gray-800/50">
            <MessageList
              messages={messages}
              containerRef={containerRef}
              hasMore={hasMore}
              loading={loading}
              onLoadMore={loadOlder}
            />
          </div>

          <div className="p-3 md:p-4 border-t border-gray-700 bg-gray-900/60 backdrop-blur">
            <div className="bg-gray-900 rounded-2xl px-2 py-2">
              {peerUserId ? (
                <div className="flex items-end gap-2">
                  <InputField
                    value={text}
                    onChange={(e) => setText(e.target.value)}
                    placeholder="Enter a message…"
                    className="bg-gray-900 text-gray-200 border border-gray-700 focus:ring-green-500 focus:border-green-500"
                    containerClass="flex-1"
                    useBaseClasses={true}
                  />
                  <div className="w-fit">
                    <Button
                      onClick={onSend}
                      isLoading={isSending}
                      disabled={!canSend}
                      variant="none"
                      className="!py-3 !px-3 bg-green-600 rounded-full cursor-pointer"
                      useBaseClasses={false}
                    >
                      <SendMessageIcon className='w-6 h-6' />
                    </Button>
                  </div>
                </div>
              ) : (
                <div className="bg-gray-900 border border-gray-700 rounded-2xl px-3 py-2 text-sm text-gray-400">
                  Select a chat to start messaging
                </div>
              )}
            </div>
          </div>
        </>
      )}
    </div>
  );
};