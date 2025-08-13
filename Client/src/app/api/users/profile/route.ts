import { NextResponse } from 'next/server';
import { cookies } from 'next/headers';

export async function GET() {
  const cookieStore = await cookies();
  const token = cookieStore.get('auth-token')?.value;
  
  if (!token) {
    return NextResponse.json(
      { valid: false, error: 'No auth token found' },
      { status: 401 }
    );
  }

  try {
    const profileUrl = `${process.env.PUBLIC_API_URL}/api/users/profile`;
    
    const response = await fetch(profileUrl, {
      method: 'GET',
      headers: { 
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}` 
      },
      cache: 'no-store'
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      return NextResponse.json(
        { 
          error: errorData.message || 'Failed to fetch profile',
          status: response.status
        },
        { status: response.status }
      );
    }

    const profile = await response.json();
    return NextResponse.json(profile);
  } catch (error) {
    return NextResponse.json(
      { error: `Internal server error ${error}` },
      { status: 500 }
    );
  }
}

export async function PUT(request: Request) {
  const cookieStore = await cookies();
  const token = cookieStore.get('auth-token')?.value;
  
  if (!token) {
    return NextResponse.json(
      { valid: false, error: 'No auth token found' },
      { status: 401 }
    );
  }

  try {
    const profileData = await request.json();
    const profileUrl = `${process.env.PUBLIC_API_URL}/api/users/profile`;
    
    const response = await fetch(profileUrl, {
      method: 'PUT',
      headers: { 
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}` 
      },
      body: JSON.stringify(profileData)
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      return NextResponse.json(
        { 
          error: errorData.message || 'Failed to update profile',
          details: errorData.errors,
          status: response.status
        },
        { status: response.status }
      );
    }

    const updatedProfile = await response.json();
    return NextResponse.json(updatedProfile);
  } catch (error) {
    return NextResponse.json(
      { error: `Internal server error ${error}` },
      { status: 500 }
    );
  }
}