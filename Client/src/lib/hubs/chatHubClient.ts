'use client';

import * as signalR from '@microsoft/signalr';

const SERVER_TIMEOUT_MS = 30_000;
const KEEP_ALIVE_MS = 15_000;

function getApiBase() {
  const fromEnv = process.env.PUBLIC_API_URL;
  return (fromEnv ?? '').replace(/\/+$/, '');
}
const API_BASE = getApiBase();
const HUB_URL = (API_BASE ? API_BASE : '') + '/hubs/chat';

async function fetchAccessToken(): Promise<string> {
  try {
    const r = await fetch('/api/auth/token', { cache: 'no-store', credentials: 'include' });
    if (!r.ok) return '';
    const data = await r.json();
    return data?.token ?? '';
  } catch {
    return '';
  }
}

let connection: signalR.HubConnection | null = null;
let startPromise: Promise<void> | null = null;

const reconnectedHandlers = new Set<() => void | Promise<void>>();

function isConnected(conn: signalR.HubConnection): boolean {
  return conn.state === signalR.HubConnectionState.Connected;
}

function notifyReconnected() {
  for (const handler of reconnectedHandlers) {
    void handler();
  }
}

function waitForConnected(conn: signalR.HubConnection, timeoutMs = 10_000): Promise<boolean> {
  if (isConnected(conn)) return Promise.resolve(true);

  return new Promise((resolve) => {
    const startedAt = Date.now();
    const timer = setInterval(() => {
      if (isConnected(conn)) {
        clearInterval(timer);
        resolve(true);
        return;
      }

      if (Date.now() - startedAt >= timeoutMs) {
        clearInterval(timer);
        resolve(false);
      }
    }, 100);
  });
}

export function getChatConnection(): signalR.HubConnection {
  if (connection) return connection;

  connection = new signalR.HubConnectionBuilder()
    .withUrl(HUB_URL, {
      accessTokenFactory: async () => await fetchAccessToken(),
      transport:
        signalR.HttpTransportType.WebSockets |
        signalR.HttpTransportType.ServerSentEvents |
        signalR.HttpTransportType.LongPolling,
      withCredentials: false,
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (ctx) =>
        Math.min(1000 * Math.pow(2, ctx.previousRetryCount), 30_000),
    })
    .configureLogging(signalR.LogLevel.Information)
    .build();

  connection.serverTimeoutInMilliseconds = SERVER_TIMEOUT_MS;
  connection.keepAliveIntervalInMilliseconds = KEEP_ALIVE_MS;

  connection.onreconnected(() => {
    notifyReconnected();
  });

  return connection!;
}

export async function ensureChatConnectionStarted(): Promise<boolean> {
  const conn = getChatConnection();

  if (isConnected(conn)) return true;

  if (
    conn.state === signalR.HubConnectionState.Connecting ||
    conn.state === signalR.HubConnectionState.Reconnecting
  ) {
    return waitForConnected(conn);
  }

  if (!startPromise) {
    startPromise = conn
      .start()
      .catch((e) => {
        console.error('Hub start failed:', e);
        throw e;
      })
      .finally(() => {
        startPromise = null;
      });
  }

  try {
    await startPromise;
  } catch {
    // already logged above
  }

  return isConnected(conn);
}

export function subscribeChatReconnected(handler: () => void | Promise<void>): () => void {
  reconnectedHandlers.add(handler);
  return () => {
    reconnectedHandlers.delete(handler);
  };
}
