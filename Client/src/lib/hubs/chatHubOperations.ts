import * as signalR from '@microsoft/signalr';
import { makeDmKey } from '@/lib/utils/makeDmKey';

export type JoinTarget =
  | { mode: 'chat'; key: string; chatId: string }
  | { mode: 'dm'; key: string; peerId: string };

export type JoinedTarget = {
  mode: 'chat' | 'dm';
  key: string;
  chatId?: string;
  peerId?: string;
};

export function getJoinTarget(params: {
  chatId?: string;
  peerUserId?: string;
  currentUserId?: string;
}): JoinTarget | null {
  const { chatId, peerUserId, currentUserId } = params;
  if (chatId) return { mode: 'chat', key: `chat:${chatId}`, chatId };
  if (peerUserId && currentUserId) {
    return { mode: 'dm', key: makeDmKey(currentUserId, peerUserId), peerId: peerUserId };
  }
  if (peerUserId) {
    return { mode: 'dm', key: `dm:${peerUserId}`, peerId: peerUserId };
  }
  return null;
}

export async function ensureConnected(conn: signalR.HubConnection): Promise<boolean> {
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    try {
      await conn.start();
    } catch (e) {
      console.error('Hub start failed:', e);
      return false;
    }
  }

  return conn.state === signalR.HubConnectionState.Connected;
}

export async function leaveTarget(conn: signalR.HubConnection, joined: JoinedTarget | null) {
  if (!joined) return;
  try {
    if (joined.mode === 'chat' && joined.chatId) {
      await conn.invoke('LeaveChat', joined.chatId);
    } else if (joined.mode === 'dm' && joined.peerId) {
      await conn.invoke('LeaveDirect', joined.peerId);
    }
  } catch (e) {
    console.warn('Leave group failed:', e);
  }
}

export async function joinTarget(conn: signalR.HubConnection, target: JoinTarget) {
  try {
    if (target.mode === 'chat') {
      await conn.invoke('JoinChat', target.chatId);
    } else {
      await conn.invoke('JoinDirect', target.peerId);
    }
  } catch (e) {
    console.error('Join failed:', e);
    throw e;
  }
}
