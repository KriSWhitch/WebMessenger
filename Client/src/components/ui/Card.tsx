import { clsx } from 'clsx';
import React from 'react';

interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  children: React.ReactNode;
  className?: string;
}

export const Card = ({ children, className, ...props }: CardProps) => {
  return (
    <div
      className={clsx(
        'bg-gray-800 rounded-xl shadow-lg border border-gray-700',
        className
      )}
      {...props}
    >
      {children}
    </div>
  );
};