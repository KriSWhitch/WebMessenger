import { clsx } from 'clsx';
import React from 'react';

interface InputFieldProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  containerClass?: string;
  showError?: boolean;
  useBaseClasses?: boolean;
  icon?: React.ReactNode;
}

export const InputField = React.forwardRef<HTMLInputElement, InputFieldProps>(
  ({ label = null, error, className, containerClass, showError = true, useBaseClasses = true, icon, ...props }, ref) => {
    return (
      <div className={clsx('space-y-1 relative', containerClass)}>
        {label && <label className="block text-gray-300 text-sm font-medium">{label}</label>}
        {icon && icon}
        <input
          ref={ref}
          className={clsx(
            useBaseClasses 
              ? 'w-full p-3 bg-gray-700 border rounded-lg focus:ring-1 focus:ring-green-500 focus:border-green-500 text-gray-200 placeholder-gray-500'
              : '',
            error ? 'border-red-500' : 'border-gray-600',
            icon ? 'pl-10 flex items-center' : '',
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