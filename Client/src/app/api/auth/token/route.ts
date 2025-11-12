import { cookies } from "next/headers";
import { NextResponse } from "next/server";

export async function GET() {
  const jar = await cookies();
  const jwt = jar.get("auth-token")?.value;
  if (!jwt) {
    return NextResponse.json({ error: "No auth token" }, { status: 401 });
  }
  return NextResponse.json({ token: jwt });
}
