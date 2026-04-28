import type { Metadata } from 'next';
import { Inter } from 'next/font/google';
import { AuthProvider } from '@/providers/AuthProvider';
import { UserProvider } from '@/providers/UserProvider';
import '../styles/globals.scss';

const inter = Inter({ subsets: ['latin'] });

export const metadata: Metadata = {
  title: 'Web Messenger',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" className="bg-gray-900">
      <body className={`${inter.className} bg-gray-900 text-gray-200`} suppressHydrationWarning>
        <AuthProvider>
          <UserProvider>
            <main className="min-h-screen">{children}</main>
          </UserProvider>
        </AuthProvider>
      </body>
    </html>
  );
}
