import React from 'react';
import clsx from 'clsx';

interface TextAreaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  error?: string;
  containerClass?: string;
}

export const TextArea = React.forwardRef<HTMLTextAreaElement, TextAreaProps>(
  ({ label, error, containerClass, className, ...props }, ref) => {
    return (
      <div className={containerClass}>
        {label && (
          <label className="block text-sm font-medium text-gray-300 mb-1">
            {label}
          </label>
        )}
        <textarea
          ref={ref}
          className={clsx(
            'w-full px-3 py-2 bg-gray-800 border rounded-lg focus:outline-none focus:ring-2',
            'text-gray-200 placeholder-gray-500 transition-all duration-200',
            'focus:ring-green-500 focus:border-transparent',
            error ? 'border-red-500' : 'border-gray-700 hover:border-gray-600',
            className
          )}
          {...props}
        />
        {error && (
          <p className="mt-1 text-sm text-red-500">{error}</p>
        )}
      </div>
    );
  }
);

TextArea.displayName = 'TextArea';