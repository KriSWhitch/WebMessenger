import '../../styles/globals.scss';
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Auth Page'
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div>{children}</div>
  );
}