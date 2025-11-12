'use client';
import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { getChatConnection } from '@/lib/hubs/chatHubClient';

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

export function useChatRealtime(params: {
  chatId?: string;
  peerUserId?: string;
  onMessage: (m: MessageCreated) => void;
  onTyping?: (p: { chatId: string; userId: string; isTyping: boolean }) => void;
}) {
  const { chatId, peerUserId, onMessage, onTyping } = params;
  const connRef = useRef<signalR.HubConnection | null>(null);
  const lastJoinKeyRef = useRef<string | undefined>(undefined);

  useEffect(() => {
    const conn = getChatConnection();
    connRef.current = conn;
    conn.on('MessageCreated', onMessage);
    if (onTyping) conn.on('Typing', onTyping);

    const doJoin = async () => {
      const key = chatId ? `chat:${chatId}` : peerUserId ? `dm:${peerUserId}` : undefined;
      if (!key || lastJoinKeyRef.current === key) return;

      if (conn.state === signalR.HubConnectionState.Disconnected) {
        try {
          await conn.start();
        } catch (e) {
          console.error('Hub start failed:', e);
          return;
        }
      }

      if (conn.state !== signalR.HubConnectionState.Connected) return;

      try {
        if (chatId) {
          await conn.invoke('JoinChat', chatId);
          lastJoinKeyRef.current = key;
        } else if (peerUserId) {
          await conn.invoke('JoinDirect', peerUserId);
          lastJoinKeyRef.current = key;
        }
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
      conn.onreconnected(() => {});
    };
  }, [chatId, peerUserId, onMessage, onTyping]);
}