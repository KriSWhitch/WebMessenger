import { NextRequest, NextResponse } from 'next/server';

export async function POST(req: NextRequest) {
  const body = await req.json().catch(() => ({}));
  const upstreamResp = await fetch(`${process.env.API_BASE_URL ?? process.env.PUBLIC_API_URL}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });

  const status = upstreamResp.status;
  const contentType = upstreamResp.headers.get('content-type') || '';
  const isJson = contentType.includes('application/json');
  const payload = isJson ? await upstreamResp.json().catch(() => ({})) : await upstreamResp.text();

  if (!upstreamResp.ok) {
    return NextResponse.json(
      typeof payload === 'string' ? { message: payload } : payload,
      { status }
    );
  }

  const token = typeof payload === 'string' ? undefined : payload?.token;
  const res = NextResponse.json({ message: 'Login successful' }, { status });

  if (token) {
    res.cookies.set('auth-token', token, {
      httpOnly: true,
      secure: process.env.NODE_ENV === 'production',
      sameSite: 'strict',
      path: '/',
      maxAge: 60 * 60 * 24, // 1 day
    });
  }

  return res;
}