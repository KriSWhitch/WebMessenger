import { cookies } from 'next/headers';
import { NextResponse } from 'next/server';

const PUBLIC_API_URL = process.env.PUBLIC_API_URL;
if (!PUBLIC_API_URL) {
  throw new Error('PUBLIC_API_URL is not found in .env.*');
}

async function getTokenFromCookies(): Promise<string | undefined> {
  const cookieStore = await cookies();
  return cookieStore.get('auth-token')?.value;
}

type ProxyOptions = {
  searchParams?: URLSearchParams;
  body?: unknown;
  headers?: Record<string, string>;
  requireAuth?: boolean;
  timeoutMs?: number;
};

function buildUrl(path: string, sp?: URLSearchParams) {
  const url = new URL(path, PUBLIC_API_URL);
  if (sp) {
    sp.forEach((v, k) => url.searchParams.set(k, v));
  }
  return url;
}

function withTimeout(timeoutMs = 0) {
  if (!timeoutMs || timeoutMs <= 0) return undefined;
  const controller = new AbortController();
  setTimeout(() => controller.abort(), timeoutMs);
  return controller;
}

export async function proxyRequest(
  method: 'GET' | 'POST' | 'PUT' | 'DELETE',
  path: string,
  opts: ProxyOptions = {}
) {
  const url = buildUrl(path, opts.searchParams);

  const headers: Record<string, string> = {
    ...(opts.headers ?? {}),
  };

  if (opts.requireAuth !== false) {
    const token = await getTokenFromCookies();
    if (!token) {
      return NextResponse.json({ valid: false, error: 'No auth token found' }, { status: 401 });
    }
    headers['Authorization'] = `Bearer ${token}`;
  }

  const isFormData = typeof FormData !== 'undefined' && opts.body instanceof FormData;
  const hasBody = opts.body !== undefined && method !== 'GET';

  if (hasBody && !isFormData && !headers['Content-Type']) {
    headers['Content-Type'] = 'application/json';
  }

  const controller = withTimeout(opts.timeoutMs);

  try {
    const resp = await fetch(url.toString(), {
      method,
      headers,
      body: hasBody
        ? isFormData
          ? (opts.body as FormData)
          : JSON.stringify(opts.body)
        : undefined,
      cache: 'no-store',
      signal: controller?.signal,
    });

    const status = resp.status;
    const contentType = resp.headers.get('content-type') || '';

    if (contentType.includes('application/json')) {
      const data = await resp.json().catch(() => ({}));
      return NextResponse.json(data, { status });
    }

    const arrayBuffer = await resp.arrayBuffer();

    const proxyHeaders: Record<string, string> = {};
    const cd = resp.headers.get('content-disposition');
    if (cd) proxyHeaders['content-disposition'] = cd;
    proxyHeaders['content-type'] = contentType || 'application/octet-stream';

    return new NextResponse(arrayBuffer, {
      status,
      headers: proxyHeaders,
    });
  } catch (err: any) {
    const aborted = err?.name === 'AbortError';
    const message = aborted ? 'Upstream timeout' : `Upstream ${method} failed: ${String(err)}`;
    const code = aborted ? 504 : 500;
    return NextResponse.json({ error: message }, { status: code });
  }
}

export function proxyGet(
  path: string,
  searchParams?: URLSearchParams,
  requireAuth = true,
  timeoutMs?: number
) {
  return proxyRequest('GET', path, { searchParams, requireAuth, timeoutMs });
}

export function proxyPost(
  path: string,
  body?: unknown,
  requireAuth = true,
  searchParams?: URLSearchParams,
  timeoutMs?: number
) {
  return proxyRequest('POST', path, { body, searchParams, requireAuth, timeoutMs });
}

export function proxyPut(
  path: string,
  body?: unknown,
  requireAuth = true,
  searchParams?: URLSearchParams,
  timeoutMs?: number
) {
  return proxyRequest('PUT', path, { body, searchParams, requireAuth, timeoutMs });
}

export function proxyDelete(
  path: string,
  searchParams?: URLSearchParams,
  requireAuth = true,
  timeoutMs?: number
) {
  return proxyRequest('DELETE', path, { searchParams, requireAuth, timeoutMs });
}

export default { proxyGet, proxyPost, proxyPut, proxyDelete };
