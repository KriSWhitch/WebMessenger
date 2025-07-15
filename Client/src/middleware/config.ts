import { authMiddleware } from './auth';

export const middleware = authMiddleware;

export const config = {
  matcher: ['/((?!_next/static|_next/image|favicon.ico).*)'],
};