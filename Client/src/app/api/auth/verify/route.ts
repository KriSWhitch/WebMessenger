import { NextResponse } from 'next/server';
import { cookies } from 'next/headers';

export async function GET() {
  try {
    const cookieStore = await cookies();
    const token = cookieStore.get('auth-token')?.value;
    
    if (!token) {
      return NextResponse.json(
        { valid: false, error: 'No auth token found' },
        { status: 401 }
      );
    }

    const verifyUrl = `${process.env.PUBLIC_API_URL}/api/auth/verify`;
    const response = await fetch(verifyUrl, {
      method: 'GET',
      headers: { 
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      credentials: 'include',
      cache: 'no-store'
    });

    if (!response.ok) {
      if (response.status === 401) {
        return NextResponse.json(
          { valid: false },
          { status: 401 }
        );
      }
      
      const errorData = await response.json().catch(() => ({}));
      return NextResponse.json(
        { 
          valid: false,
          error: errorData.message || 'Verification failed',
          status: response.status
        },
        { status: response.status }
      );
    }

    const data = await response.json();
    return NextResponse.json(
      { valid: true, ...data },
      { status: 200 }
    );

  } catch (error) {
    console.error('Verification error:', error);
    return NextResponse.json(
      { 
        valid: false,
        error: 'Internal server error',
        details: error instanceof Error ? error.message : String(error)
      },
      { status: 500 }
    );
  }
}

export async function OPTIONS() {
  return NextResponse.json({}, {
    headers: {
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Methods': 'GET, OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type, Authorization',
    }
  });
}