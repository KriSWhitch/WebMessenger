'use client';
import { ReactNode, MouseEvent } from 'react';

interface DropdownMenuItemProps {
  children: ReactNode;
  onClick: (e: MouseEvent) => void;
  className?: string;
  danger?: boolean;
}

export const DropdownMenuItem = ({
  children,
  onClick,
  className = '',
  danger = false
}: DropdownMenuItemProps) => {
  return (
    <button
      onClick={onClick}
      className={`w-full text-left px-3 py-2 text-sm hover:bg-gray-700 rounded transition-colors duration-150 ${
        danger ? 'text-red-400' : ''
      } ${className}`}
    >
      {children}
    </button>
  );
};