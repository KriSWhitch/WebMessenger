'use client';
import { useState, useCallback } from 'react';
import { InputField } from '@/components/ui/Input/Input';
import { Button } from '@/components/ui/Button/Button';
import { SendMessageIcon } from '@/components/icons/SendMessageIcon';
import type { MessageVM } from './MessageBubble';

interface MessageComposerProps {
  chatId: string | null;
  meId: string | null | undefined;
  peerUserId: string;
  setMessages: React.Dispatch<React.SetStateAction<MessageVM[]>>;
  isNearBottom: () => boolean;
  scrollToBottom: () => void;
  onChatIdResolved: (chatId: string) => void;
}

export function MessageComposer({
  chatId,
  meId,
  peerUserId,
  setMessages,
  isNearBottom,
  scrollToBottom,
  onChatIdResolved,
}: MessageComposerProps) {
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
      if (!chatId) onChatIdResolved(data.chatId);
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
  }, [canSend, chatId, meId, peerUserId, text, setMessages, isNearBottom, scrollToBottom, onChatIdResolved]);

  if (!peerUserId) {
    return (
      <div className="bg-gray-900 border border-gray-700 rounded-2xl px-3 py-2 text-sm text-gray-400">
        Select a chat to start messaging
      </div>
    );
  }

  return (
    <div className="flex items-end gap-2">
      <InputField
        value={text}
        onChange={(e) => setText(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            onSend();
          }
        }}
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
  );
}
