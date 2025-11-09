import { cookies } from 'next/headers';
import { NextResponse } from 'next/server';

const API_BASE_URL = process.env.PUBLIC_API_URL;
if (!API_BASE_URL) {
  throw new Error('API_BASE_URL is not found in .env.*');
}

async function getTokenFromCookies() {
  const cookieStore = await cookies();
  return cookieStore.get('auth-token')?.value;
}

type ProxyOptions = {
  searchParams?: URLSearchParams;
  body?: unknown;
  headers?: Record<string, string>;
  requireAuth?: boolean;
};

async function proxyRequest(method: string, path: string, opts: ProxyOptions = {}) {
  const url = new URL(path, API_BASE_URL);
  if (opts.searchParams) {
    opts.searchParams.forEach((v, k) => url.searchParams.set(k, v));
  }

  const headers: Record<string, string> = { ...(opts.headers ?? {}) };

  if (opts.requireAuth !== false) {
    const token = await getTokenFromCookies();
    if (!token) {
      return NextResponse.json({ valid: false, error: 'No auth token found' }, { status: 401 });
    }
    headers['Authorization'] = `Bearer ${token}`;
  }

  const isFormData = typeof FormData !== 'undefined' && opts.body instanceof FormData;
  const hasJsonBody = opts.body !== undefined && !isFormData;

  if (hasJsonBody && !headers['Content-Type']) {
    headers['Content-Type'] = 'application/json';
  }

  try {
    const resp = await fetch(url.toString(), {
      method,
      headers,
      body: opts.body
        ? isFormData
          ? (opts.body as FormData)
          : JSON.stringify(opts.body)
        : undefined,
    });

    const status = resp.status;
    const contentType = resp.headers.get('content-type') || '';

    if (contentType.includes('application/json')) {
      const data = await resp.json().catch(() => ({}));
      return NextResponse.json(data, { status });
    }

    const arrayBuffer = await resp.arrayBuffer();
    return new NextResponse(arrayBuffer, {
      status,
      headers: { 'content-type': contentType || 'application/octet-stream' },
    });
  } catch (err) {
    return NextResponse.json(
      { error: `Upstream ${method} failed: ${String(err)}` },
      { status: 500 }
    );
  }
}

export function proxyGet(path: string, searchParams?: URLSearchParams, requireAuth = true) {
  return proxyRequest('GET', path, { searchParams, requireAuth });
}
export function proxyPost(path: string, body?: unknown, requireAuth = true, searchParams?: URLSearchParams) {
  return proxyRequest('POST', path, { body, searchParams, requireAuth });
}
export function proxyPut(path: string, body?: unknown, requireAuth = true, searchParams?: URLSearchParams) {
  return proxyRequest('PUT', path, { body, searchParams, requireAuth });
}
export function proxyDelete(path: string, searchParams?: URLSearchParams, requireAuth = true) {
  return proxyRequest('DELETE', path, { searchParams, requireAuth });
}

export default { proxyGet, proxyPost, proxyPut, proxyDelete };