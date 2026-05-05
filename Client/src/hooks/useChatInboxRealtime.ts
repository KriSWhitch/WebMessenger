'use client';
import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { getChatConnection, subscribeChatReconnected } from '@/lib/hubs/chatHubClient';
import { ChatHubEvents } from '@/lib/hubs/chatHubContracts';
import { ensureConnected, getJoinTarget, joinTarget } from '@/lib/hubs/chatHubOperations';
import type { MessageCreatedPayload, ReadReceiptPayload, TypingPayload } from '@/types/chat';

type MessageCreated = MessageCreatedPayload;
type ReadReceipt = ReadReceiptPayload;

export function useChatRealtime(params: {
  chatId?: string;
  peerUserId?: string;
  onMessage: (m: MessageCreated) => void;
  onTyping?: (p: TypingPayload) => void;
  onReadReceipt?: (p: ReadReceipt) => void;
}) {
  const { chatId, peerUserId, onMessage, onTyping, onReadReceipt } = params;
  const connRef = useRef<signalR.HubConnection | null>(null);
  const lastJoinKeyRef = useRef<string | undefined>(undefined);

  useEffect(() => {
    const conn = getChatConnection();
    connRef.current = conn;

    conn.on(ChatHubEvents.MessageCreated, onMessage);
    if (onTyping) conn.on(ChatHubEvents.Typing, onTyping);
    if (onReadReceipt) conn.on(ChatHubEvents.ReadReceipt, onReadReceipt);

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
    const unsubscribeReconnected = subscribeChatReconnected(onReconnected);

    return () => {
      conn.off(ChatHubEvents.MessageCreated, onMessage);
      if (onTyping) conn.off(ChatHubEvents.Typing, onTyping);
      if (onReadReceipt) conn.off(ChatHubEvents.ReadReceipt, onReadReceipt);
      unsubscribeReconnected();
    };
  }, [chatId, peerUserId, onMessage, onTyping, onReadReceipt]);
}
