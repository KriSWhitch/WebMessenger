'use client';
import React, { useCallback, useMemo, useRef, useState } from 'react';
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
import { useMarkRead } from '@/hooks/useMarkRead';

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
  const chatViewOpen = !!selectedChat;
  const [chatId, setChatId] = useState<string | null>(getServerChatId(selectedChat));
  const [meId, setMeId] = useState<string | null>(null);

  const peerUserId = useMemo(() => {
    if (!selectedChat) return '';
    if (selectedChat.peerUserId) return selectedChat.peerUserId;

    if (meId && selectedChat.members?.length) {
      const other = selectedChat.members.find((m) => m.userId !== meId);
      if (other) return other.userId;
    }

    return '';
  }, [selectedChat, meId]);

  React.useEffect(() => {
    let alive = true;
    (async () => {
      try {
        const r = await fetch('/api/users/profile', { cache: 'no-store', credentials: 'include' });
        if (!r.ok) return;
        const profile = await r.json();
        if (alive) setMeId(profile?.id ?? null);
      } catch {}
    })();
    return () => {
      alive = false;
    };
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

  const { markRead } = useMarkRead();

  const lastMarkedUpToRef = useRef<number>(0);
  const inflightRef = useRef<boolean>(false);
  const readTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const clearReadTimer = () => {
    if (readTimerRef.current) {
      clearTimeout(readTimerRef.current);
      readTimerRef.current = null;
    }
  };

  const getMaxUnreadTs = useCallback((): number | null => {
    if (!chatId || messages.length === 0) return null;
    let maxTs: number | null = null;
    for (const m of messages) {
      if (m.chatId !== chatId) continue;
      const isMine = meId ? m.senderId === meId : false;
      if (isMine) continue;
      if (m.isRead) continue;
      const ts = new Date(m.sentAt).getTime();
      if (Number.isFinite(ts)) {
        if (maxTs === null || ts > maxTs) maxTs = ts;
      }
    }
    return maxTs;
  }, [chatId, messages, meId]);

  const scheduleReadIfNeeded = useCallback(() => {
    if (!chatViewOpen) return;
    if (!chatId) return;
    const visible = typeof document !== 'undefined' ? document.visibilityState === 'visible' : true;
    if (!visible) return;
    if (!isNearBottom()) return;

    const maxUnreadTs = getMaxUnreadTs();
    if (!maxUnreadTs) return;
    if (maxUnreadTs <= lastMarkedUpToRef.current) return;

    clearReadTimer();
    readTimerRef.current = setTimeout(async () => {
      if (!chatViewOpen) return;
      if (!chatId) return;
      if (inflightRef.current) return;
      if (!isNearBottom()) return;

      const maxNow = getMaxUnreadTs();
      if (!maxNow || maxNow <= lastMarkedUpToRef.current) return;

      inflightRef.current = true;
      try {
        setMessages((prev) =>
          prev.map((m) =>
            m.chatId === chatId &&
            !(meId ? m.senderId === meId : false) &&
            !m.isRead &&
            new Date(m.sentAt).getTime() <= maxNow
              ? { ...m, isRead: true }
              : m
          )
        );
        lastMarkedUpToRef.current = maxNow;

        const rs = await markRead(chatId);
        if (rs?.lastReadAt) {
          const serverTs = new Date(rs.lastReadAt).getTime();
          if (Number.isFinite(serverTs) && serverTs > lastMarkedUpToRef.current) {
            lastMarkedUpToRef.current = serverTs;
          }
          onChatRead?.(chatId, rs.lastReadAt, rs.unreadCount ?? 0);
        }
      } catch {
        // noop
      } finally {
        inflightRef.current = false;
      }
    }, 400);
  }, [chatViewOpen, chatId, getMaxUnreadTs, isNearBottom, onChatRead, setMessages, meId, markRead]);

  React.useEffect(() => clearReadTimer, [chatId]);

  React.useEffect(() => {
    const handler = () => {
      if (!chatViewOpen) return;
      if (!chatId) return;
      scheduleReadIfNeeded();
    };
    document.addEventListener('visibilitychange', handler);
    return () => document.removeEventListener('visibilitychange', handler);
  }, [chatViewOpen, chatId, scheduleReadIfNeeded]);

  React.useEffect(() => {
    if (!chatViewOpen) return;
    if (!chatId) return;
    lastMarkedUpToRef.current = 0;
    scheduleReadIfNeeded();
  }, [chatViewOpen, chatId, scheduleReadIfNeeded]);

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
      (p) => {
        if (!chatViewOpen) return;
        if (p.chatId && p.chatId === chatId && p.userId === meId) {
          onChatRead?.(p.chatId, p.lastReadAt, 0);
        }
      },
      [chatViewOpen, chatId, meId, onChatRead]
    ),
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
    setMessages((prev) => [...prev, optimistic]);
    setText('');
    setTimeout(() => {
      if (isNearBottom()) scrollToBottom();
    }, 0);
    try {
      const res = await fetch(`/api/chats/direct/${peerUserId}/messages`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ content: optimistic.content }),
        credentials: 'include',
      });
      if (!res.ok) {
        setMessages((prev) =>
          prev.map((m) => (m.id === clientId ? { ...m, _pending: false, _failed: true } : m))
        );
        return;
      }
      const data = (await res.json()) as { chatId: string; message: MessageVM };
      setMessages((prev) => prev.filter((m) => m.id !== clientId));
      if (!chatId) setChatId(data.chatId);
      setTimeout(() => {
        if (isNearBottom()) scrollToBottom();
      }, 0);
    } catch {
      setMessages((prev) =>
        prev.map((m) => (m.id === clientId ? { ...m, _pending: false, _failed: true } : m))
      );
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
                  <SendMessageIcon className="w-6 h-6" />
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
    </div>
  );
};
