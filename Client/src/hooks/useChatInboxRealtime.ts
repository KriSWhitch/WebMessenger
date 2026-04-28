'use client';
import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { getChatConnection } from '@/lib/hubs/chatHubClient';
import { ensureConnected, getJoinTarget, joinTarget } from '@/lib/hubs/chatHubOperations';

type MessageCreated = {
  chatId: string;
  message: {
    id: string;
    chatId: string;
    senderId: string;
    content: string;
    sentAt: string;
    editedAt?: string | null;
    isRead: boolean;
  };
};

type ReadReceipt = {
  chatId: string;
  userId: string;
  lastReadAt: string;
};

export function useChatRealtime(params: {
  chatId?: string;
  peerUserId?: string;
  onMessage: (m: MessageCreated) => void;
  onTyping?: (p: { chatId: string; userId: string; isTyping: boolean }) => void;
  onReadReceipt?: (p: ReadReceipt) => void;
}) {
  const { chatId, peerUserId, onMessage, onTyping, onReadReceipt } = params;
  const connRef = useRef<signalR.HubConnection | null>(null);
  const lastJoinKeyRef = useRef<string | undefined>(undefined);

  useEffect(() => {
    const conn = getChatConnection();
    connRef.current = conn;

    conn.on('MessageCreated', onMessage);
    if (onTyping) conn.on('Typing', onTyping);
    if (onReadReceipt) conn.on('ReadReceipt', onReadReceipt);

    const doJoin = async () => {
      const target = getJoinTarget({ chatId, peerUserId });
      if (!target || lastJoinKeyRef.current === target.key) return;

      if (!(await ensureConnected(conn))) return;

      try {
        await joinTarget(conn, target);
        lastJoinKeyRef.current = target.key;
      } catch (e) {
        console.error('Join failed:', e);
      }
    };

    void doJoin();

    const onReconnected = async () => {
      lastJoinKeyRef.current = undefined;
      await doJoin();
    };
    conn.onreconnected(onReconnected);

    return () => {
      conn.off('MessageCreated', onMessage);
      if (onTyping) conn.off('Typing', onTyping);
      if (onReadReceipt) conn.off('ReadReceipt', onReadReceipt);
      conn.onreconnected(() => {});
    };
  }, [chatId, peerUserId, onMessage, onTyping, onReadReceipt]);
}
