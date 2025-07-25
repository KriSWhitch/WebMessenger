import { NextResponse } from 'next/server';

export async function GET(req: Request) {
    return NextResponse.json(req);
}

export async function POST(req: Request) {
  try {
    const body = await req.json();
    const registerUrl = `${process.env.PUBLIC_API_URL}/api/auth/register`;

    const response = await fetch(registerUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(body)
    });

    if (!response.ok) {
      const error = await response.json();
      return NextResponse.json(
        { error: error.message },
        { status: response.status }
      );
    }

    const data = await response.json();
    return NextResponse.json(data);
    
  } catch (err) {
    return NextResponse.json(
      { error: `Internal Server Error: ${err}` },
      { status: 500 }
    );
  }
}