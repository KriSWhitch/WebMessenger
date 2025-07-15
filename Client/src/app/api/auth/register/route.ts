import { NextResponse } from 'next/server';

export async function GET(req: Request) {
    return NextResponse.json(req);
}

export async function POST(req: Request) {
  try {
    const body = await req.json();
    
    const apiUrl = `${process.env.PUBLIC_API_URL}/api/auth/register`;
    const apiRes = await fetch(apiUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(body)
    });

    if (!apiRes.ok) {
      const error = await apiRes.json();
      return NextResponse.json(
        { error: error.message },
        { status: apiRes.status }
      );
    }

    const data = await apiRes.json();
    return NextResponse.json(data);
    
  } catch (err) {
    return NextResponse.json(
      { error: `Internal Server Error: ${err}` },
      { status: 500 }
    );
  }
}