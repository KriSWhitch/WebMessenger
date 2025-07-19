import { clsx } from 'clsx';
import React, { forwardRef } from 'react';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  isLoading?: boolean;
  variant?: 'primary' | 'secondary' | 'danger' | 'none';
  useBaseClasses?: boolean;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({
    children,
    isLoading,
    variant = 'none',
    className,
    useBaseClasses = true,
    ...props
  }, ref) => {
    const baseClasses =
      'w-full font-medium py-3 px-4 rounded-lg transition duration-200 flex justify-center items-center';

    const variantClasses = {
      primary: 'bg-green-600 hover:bg-green-500 text-white',
      secondary: 'bg-gray-600 hover:bg-gray-500 text-white',
      danger: 'bg-red-600 hover:bg-red-500 text-white',
      none: ''
    };

    return (
      <button
        ref={ref}
        className={clsx(
          useBaseClasses ? baseClasses : '',
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
  }
);

Button.displayName = 'Button';