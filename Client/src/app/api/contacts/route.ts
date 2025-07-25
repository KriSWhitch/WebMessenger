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

  try {
    const getContactsUrl = `${process.env.PUBLIC_API_URL}/api/contacts?query=${query}`;
    
    const response = await fetch(getContactsUrl, {
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

    const contacts = await response.json(); 

    return NextResponse.json(contacts);
  } catch (error) {
    return NextResponse.json(
      { error: `Failed to fetch contacts: ${error}` },
      { status: 500 }
    );
  }
}