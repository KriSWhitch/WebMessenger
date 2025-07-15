import { clsx } from 'clsx';
import React from 'react';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  isLoading?: boolean;
  variant?: 'primary' | 'secondary' | 'danger';
}

export const Button = ({
  children,
  isLoading,
  variant = 'primary',
  className,
  ...props
}: ButtonProps) => {
  const baseClasses =
    'w-full font-medium py-3 px-4 rounded-lg transition duration-200 flex justify-center items-center';

  const variantClasses = {
    primary: 'bg-green-600 hover:bg-green-500 text-white',
    secondary: 'bg-gray-600 hover:bg-gray-500 text-white',
    danger: 'bg-red-600 hover:bg-red-500 text-white',
  };

  return (
    <button
      className={clsx(
        baseClasses,
        variantClasses[variant],
        { 'opacity-75 cursor-not-allowed': isLoading },
        className
      )}
      disabled={isLoading}
      {...props}
    >
      {isLoading ? <span className="animate-spin">🌀</span> : children}
    </button>
  );
};