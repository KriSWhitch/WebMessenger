'use client';

import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { getChatConnection } from '@/lib/hubs/chatHubClient';

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
  onMessage: (m: MessageCreated) => void;
  onTyping?: (p: { chatId: string; userId: string; isTyping: boolean }) => void;
  onReadReceipt?: (p: ReadReceipt) => void;
};

export function useChatRealtime({
  chatId,
  peerUserId,
  onMessage,
  onTyping,
  onReadReceipt,
}: Params) {
  const connRef = useRef<signalR.HubConnection | null>(null);
  const joinedRef = useRef<{ mode: 'dm' | 'chat'; key: string; peerId?: string; chatId?: string } | null>(null);

  useEffect(() => {
    const conn = getChatConnection();
    connRef.current = conn;

    const handleMessage = (p: MessageCreated) => onMessage(p);
    const handleTyping = onTyping ? (p: any) => onTyping(p) : undefined;
    const handleRead = onReadReceipt ? (p: any) => onReadReceipt(p) : undefined;

    conn.off('MessageCreated', handleMessage);
    conn.on('MessageCreated', handleMessage);
    if (handleTyping) { conn.off('Typing', handleTyping); conn.on('Typing', handleTyping); }
    if (handleRead)   { conn.off('ReadReceipt', handleRead); conn.on('ReadReceipt', handleRead); }

    const ensureStarted = async () => {
      if (conn.state === signalR.HubConnectionState.Disconnected) {
        await conn.start().catch(e => { console.error('Hub start failed:', e); });
      }
      return conn.state === signalR.HubConnectionState.Connected;
    };

    const joinCurrent = async () => {
      const target = chatId ? { mode: 'chat' as const, key: `chat:${chatId}`, chatId }
                            : peerUserId ? { mode: 'dm' as const, key: `dm:${peerUserId}`, peerId: peerUserId }
                                         : null;
      if (!target) return;

      if (joinedRef.current && joinedRef.current.mode === target.mode && joinedRef.current.key === target.key) {
        return;
      }

      if (joinedRef.current) {
        try {
          if (joinedRef.current.mode === 'dm' && joinedRef.current.peerId) {
            await conn.invoke('LeaveDirect', joinedRef.current.peerId);
          } else if (joinedRef.current.mode === 'chat' && joinedRef.current.chatId) {
            await conn.invoke('LeaveChat', joinedRef.current.chatId);
          }
        } catch (e) {
          console.warn('Leave group failed:', e);
        }
      }

      if (!(await ensureStarted())) return;

      try {
        if (target.mode === 'chat' && target.chatId) {
          await conn.invoke('JoinChat', target.chatId);
          joinedRef.current = target;
        } else if (target.mode === 'dm' && target.peerId) {
          await conn.invoke('JoinDirect', target.peerId);
          joinedRef.current = target;
        }
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
      if (handleTyping)   conn.off('Typing', handleTyping);
      if (handleRead)     conn.off('ReadReceipt', handleRead);

      (async () => {
        try {
          if (joinedRef.current?.mode === 'dm' && joinedRef.current.peerId) {
            await conn.invoke('LeaveDirect', joinedRef.current.peerId);
          } else if (joinedRef.current?.mode === 'chat' && joinedRef.current.chatId) {
            await conn.invoke('LeaveChat', joinedRef.current.chatId);
          }
        } catch { /* noop */ }
        finally { joinedRef.current = null; }
      })();
    };
  }, [chatId, peerUserId, onMessage, onTyping, onReadReceipt]);
}