'use client';

import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { getChatConnection, subscribeChatReconnected } from '@/lib/hubs/chatHubClient';
import { ChatHubEvents } from '@/lib/hubs/chatHubContracts';
import { ensureConnected, getJoinTarget, joinTarget, leaveTarget } from '@/lib/hubs/chatHubOperations';
import type { MessageCreatedPayload, ReadReceiptPayload, TypingPayload } from '@/types/chat';

export type MessageCreated = MessageCreatedPayload;

export type ReadReceipt = ReadReceiptPayload;

type Params = {
  chatId?: string;
  peerUserId?: string;
  currentUserId?: string;
  onMessage: (m: MessageCreated) => void;
  onTyping?: (p: TypingPayload) => void;
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
    const handleTyping = onTyping ? (p: TypingPayload) => onTyping(p) : undefined;
    const handleRead = onReadReceipt ? (p: ReadReceipt) => onReadReceipt(p) : undefined;

    conn.off(ChatHubEvents.MessageCreated, handleMessage);
    conn.on(ChatHubEvents.MessageCreated, handleMessage);
    if (handleTyping) {
      conn.off(ChatHubEvents.Typing, handleTyping);
      conn.on(ChatHubEvents.Typing, handleTyping);
    }
    if (handleRead) {
      conn.off(ChatHubEvents.ReadReceipt, handleRead);
      conn.on(ChatHubEvents.ReadReceipt, handleRead);
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

    const unsubscribeReconnected = subscribeChatReconnected(async () => {
      joinedRef.current = null;
      await joinCurrent();
    });

    return () => {
      void leaveTarget(conn, joinedRef.current);
      joinedRef.current = null;

      conn.off(ChatHubEvents.MessageCreated, handleMessage);
      if (handleTyping) conn.off(ChatHubEvents.Typing, handleTyping);
      if (handleRead) conn.off(ChatHubEvents.ReadReceipt, handleRead);
      unsubscribeReconnected();
    };
  }, [chatId, peerUserId, currentUserId, onMessage, onTyping, onReadReceipt]);
}
