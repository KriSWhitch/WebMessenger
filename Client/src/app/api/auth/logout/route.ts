import { NextResponse } from 'next/server';

export async function POST() {
  try {
    const response = NextResponse.json({ message: 'Logout successful' });
    response.cookies.delete('auth-token');
    return response;
  } catch (error) {
    return NextResponse.json(
      { message: `Logout failed: ${error}` },
      { status: 500 }
    );
  }
}