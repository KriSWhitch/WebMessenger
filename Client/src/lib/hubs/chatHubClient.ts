'use client';

import * as signalR from '@microsoft/signalr';

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
  } catch { return ''; }
}

let connection: signalR.HubConnection | null = null;

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

  return connection!;
}