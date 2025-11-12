import React from "react";

export interface CloseIconProps extends React.SVGProps<SVGSVGElement> {
  className?: string;
  filled?: boolean;
  strokeWidth?: number;
}

export const CloseIcon: React.FC<CloseIconProps> = ({
  className,
  filled = false,
  strokeWidth = 2,
  ...props
}) => {
  if (filled) {
    return (
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="currentColor"
        className={className}
        aria-hidden={props["aria-label"] ? undefined : true}
        {...props}
      >
        <path
          fillRule="evenodd"
          d="M12 2.25a9.75 9.75 0 1 0 0 19.5 9.75 9.75 0 0 0 0-19.5Zm-2.72 5.97a.75.75 0 0 1 1.06 0L12 9.94l1.66-1.72a.75.75 0 1 1 1.08 1.04L13.06 11l1.68 1.74a.75.75 0 1 1-1.08 1.04L12 12.06l-1.66 1.72a.75.75 0 0 1-1.08-1.04L10.94 11 9.26 9.26a.75.75 0 0 1 0-1.04Z"
          clipRule="evenodd"
        />
      </svg>
    );
  }

  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={strokeWidth}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden={props["aria-label"] ? undefined : true}
      {...props}
    >
      <path d="M18 6L6 18" />
      <path d="M6 6l12 12" />
    </svg>
  );
};