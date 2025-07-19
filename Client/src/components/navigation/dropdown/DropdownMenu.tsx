'use client';
import { ReactNode, forwardRef } from 'react';
import { Card } from '@/components/ui/Card/Card';

interface DropdownMenuProps {
  isOpen: boolean;
  children: ReactNode;
  className?: string;
}

export const DropdownMenu = forwardRef<HTMLDivElement, DropdownMenuProps>(
  ({ isOpen, children, className = '' }, ref) => {
    if (!isOpen) return null;

    return (
      <Card 
        ref={ref}
        className={`absolute left-0 top-6 z-50 w-48 shadow-lg ${className}`}
      >
        {children}
      </Card>
    );
  }
);

DropdownMenu.displayName = 'DropdownMenu';