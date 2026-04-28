'use client';

import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { getChatConnection } from '@/lib/hubs/chatHubClient';
import { ensureConnected, getJoinTarget, joinTarget, leaveTarget } from '@/lib/hubs/chatHubOperations';

export type MessageCreated = {
  chatId: string;
  peerUserId?: string;
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

export type ReadReceipt = {
  chatId: string;
  userId: string;
  lastReadAt: string;
};

type Params = {
  chatId?: string;
  peerUserId?: string;
  currentUserId?: string;
  onMessage: (m: MessageCreated) => void;
  onTyping?: (p: { chatId: string; userId: string; isTyping: boolean }) => void;
  onReadReceipt?: (p: ReadReceipt) => void;
};

export function useChatRealtime({
  chatId,
  peerUserId,
  currentUserId,
  onMessage,
  onTyping,
  onReadReceipt,
}: Params) {
  const connRef = useRef<signalR.HubConnection | null>(null);
  const joinedRef = useRef<{
    mode: 'dm' | 'chat';
    key: string;
    peerId?: string;
    chatId?: string;
  } | null>(null);

  useEffect(() => {
    const conn = getChatConnection();
    connRef.current = conn;

    const handleMessage = (p: MessageCreated) => onMessage(p);
    const handleTyping = onTyping ? (p: { chatId: string; userId: string; isTyping: boolean }) => onTyping(p) : undefined;
    const handleRead = onReadReceipt ? (p: ReadReceipt) => onReadReceipt(p) : undefined;

    conn.off('MessageCreated', handleMessage);
    conn.on('MessageCreated', handleMessage);
    if (handleTyping) {
      conn.off('Typing', handleTyping);
      conn.on('Typing', handleTyping);
    }
    if (handleRead) {
      conn.off('ReadReceipt', handleRead);
      conn.on('ReadReceipt', handleRead);
    }

    const joinCurrent = async () => {
      const target = getJoinTarget({ chatId, peerUserId, currentUserId });

      if (!target) return;

      if (joinedRef.current && joinedRef.current.key === target.key) return;

      if (joinedRef.current) {
        await leaveTarget(conn, joinedRef.current);
      }

      if (!(await ensureConnected(conn))) return;

      try {
        await joinTarget(conn, target);
        joinedRef.current = target;
      } catch (e) {
        console.error('Join failed:', e);
      }
    };

    void joinCurrent();

    conn.onreconnected(async () => {
      joinedRef.current = null;
      await joinCurrent();
    });

    return () => {
      conn.off('MessageCreated', handleMessage);
      if (handleTyping) conn.off('Typing', handleTyping);
      if (handleRead) conn.off('ReadReceipt', handleRead);
    };
  }, [chatId, peerUserId, currentUserId, onMessage, onTyping, onReadReceipt]);
}
