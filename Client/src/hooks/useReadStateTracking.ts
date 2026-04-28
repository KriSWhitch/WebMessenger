import { useCallback, useEffect, useRef } from 'react';
import type { MessageVM } from '@/components/features/messenger/chat/MessageBubble';
import { useMarkRead } from '@/hooks/useMarkRead';

interface UseReadStateTrackingOptions {
  chatViewOpen: boolean;
  chatId: string | null;
  messages: MessageVM[];
  meId: string | null | undefined;
  isNearBottom: () => boolean;
  setMessages: React.Dispatch<React.SetStateAction<MessageVM[]>>;
  onChatRead?: (chatId: string, lastReadAt: string, unreadCount: number) => void;
}

export function useReadStateTracking({
  chatViewOpen,
  chatId,
  messages,
  meId,
  isNearBottom,
  setMessages,
  onChatRead,
}: UseReadStateTrackingOptions) {
  const { markRead } = useMarkRead();
  const lastMarkedUpToRef = useRef<number>(0);
  const inflightRef = useRef<boolean>(false);
  const readTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const clearReadTimer = useCallback(() => {
    if (readTimerRef.current) {
      clearTimeout(readTimerRef.current);
      readTimerRef.current = null;
    }
  }, []);

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
  }, [chatViewOpen, chatId, getMaxUnreadTs, isNearBottom, onChatRead, setMessages, meId, markRead, clearReadTimer]);

  useEffect(() => clearReadTimer, [chatId, clearReadTimer]);

  useEffect(() => {
    const handler = () => {
      if (!chatViewOpen) return;
      if (!chatId) return;
      scheduleReadIfNeeded();
    };
    document.addEventListener('visibilitychange', handler);
    return () => document.removeEventListener('visibilitychange', handler);
  }, [chatViewOpen, chatId, scheduleReadIfNeeded]);

  useEffect(() => {
    if (!chatViewOpen) return;
    if (!chatId) return;
    lastMarkedUpToRef.current = 0;
    scheduleReadIfNeeded();
  }, [chatViewOpen, chatId, scheduleReadIfNeeded]);

  return { scheduleReadIfNeeded };
}
