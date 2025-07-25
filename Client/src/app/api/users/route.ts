import { cookies } from 'next/headers';
import { NextResponse } from 'next/server';

export async function GET(request: Request) {
  const cookieStore = await cookies();
  const token = cookieStore.get('auth-token')?.value;
  
  if (!token) {
    return NextResponse.json(
      { valid: false, error: 'No auth token found' },
      { status: 401 }
    );
  }

  const { searchParams } = new URL(request.url);
  const query = searchParams.get('query') || '';
  const limit = searchParams.get('limit') || 10;

  try {
    const searchUsersUrl = `${process.env.PUBLIC_API_URL}/api/users?query=${query}&limit=${limit}`;
    
    const response = await fetch(searchUsersUrl, {
      method: 'GET',
      headers: { 
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}` 
      }
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

    const users = await response.json();

    return NextResponse.json(users);
  } catch (error) {
    return NextResponse.json(
      { error: `Failed to search users: ${error}` },
      { status: 500 }
    );
  }
}