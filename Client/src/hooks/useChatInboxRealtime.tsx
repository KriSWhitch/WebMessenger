// src/shared/realtime/useChatInboxRealtime.ts
'use client';
import { useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
import { getChatConnection } from '@/lib/hubs/chatHubClient';

type MessageCreatedPayload = {
  chatId: string;
  message: { id: string; chatId: string; senderId: string; content: string; sentAt: string; };
};

export function useChatInboxRealtime(
  onMessageCreated: (p: MessageCreatedPayload) => void,
) {
  useEffect(() => {
    const conn = getChatConnection();
    const start = async () => {
      if (conn.state === signalR.HubConnectionState.Disconnected) {
        try { await conn.start(); } catch (e) { console.error('Hub start failed:', e); }
      }
    };
    conn.on('MessageCreated', onMessageCreated);
    void start();
    const onReconnected = () => { };
    conn.onreconnected(onReconnected);
    return () => {
      conn.off('MessageCreated', onMessageCreated);
      conn.onreconnected(() => {});
    };
  }, [onMessageCreated]);
}