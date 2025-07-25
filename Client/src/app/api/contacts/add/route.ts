import { NextResponse } from 'next/server';
import { cookies } from 'next/headers';

export async function POST(request: Request) {
  const cookieStore = await cookies();
  const token = cookieStore.get('auth-token')?.value;
  
  if (!token) {
    return NextResponse.json(
      { valid: false, error: 'No auth token found' },
      { status: 401 }
    );
  }

  try {
    const body = await request.json();
    const addContactUrl = `${process.env.PUBLIC_API_URL}/api/contacts/add`;
    
    const response = await fetch(addContactUrl, {
      method: 'POST',
      headers: { 
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}` 
      },
      body: JSON.stringify(body)
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

    const result = response.json(); 

    return NextResponse.json(result);
  } catch (error) {
    return NextResponse.json(
      { error: `Failed to add contact: ${error}` },
      { status: 500 }
    );
  }
}