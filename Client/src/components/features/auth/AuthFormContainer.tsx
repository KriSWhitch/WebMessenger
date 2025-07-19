import { clsx } from 'clsx';
import React from 'react';
import { Card } from '../../ui/Card/Card';

interface AuthFormContainerProps {
  title: string;
  subtitle: string;
  children: React.ReactNode;
  footer?: React.ReactNode;
  error?: string;
  className?: string;
}

export const AuthFormContainer = ({
  title,
  subtitle,
  children,
  footer,
  error,
  className,
}: AuthFormContainerProps) => {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-900 p-4">
      <div className="w-full max-w-md">
        <Card className={clsx('p-8', className)}>
          <div className="text-center mb-8">
            <h1 className="text-3xl font-bold text-green-600">{title}</h1>
            <p className="text-gray-400 mt-2">{subtitle}</p>
          </div>

          {error && (
            <div className="mb-6 p-3 bg-red-900/20 border border-red-800 rounded-lg text-red-400 text-center">
              {error}
            </div>
          )}

          {children}

          {footer && <div className="mt-8 text-center text-gray-400">{footer}</div>}
        </Card>
      </div>
    </div>
  );
};