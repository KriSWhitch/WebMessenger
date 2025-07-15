import { clsx } from 'clsx';
import React from 'react';

interface InputFieldProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string;
  containerClass?: string;
  showError?: boolean;
}

export const InputField = React.forwardRef<HTMLInputElement, InputFieldProps>(
  ({ label, error, className, containerClass, showError = true, ...props }, ref) => {
    return (
      <div className={clsx('space-y-1', containerClass)}>
        <label className="block text-gray-300 text-sm font-medium">
          {label}
        </label>
        <input
          ref={ref}
          className={clsx(
            'w-full p-3 bg-gray-700 border rounded-lg focus:ring-1 focus:ring-green-500 focus:border-green-500 text-gray-200 placeholder-gray-500',
            error ? 'border-red-500' : 'border-gray-600',
            className
          )}
          {...props}
        />
        {showError && error && <p className="text-sm text-red-400">{error}</p>}
      </div>
    );
  }
);

InputField.displayName = 'InputField';