import React from "react";

interface LeftArrowIconProps extends React.SVGProps<SVGSVGElement> {
  className?: string;
}

export const LeftArrowIcon = ({ className, ...props }: LeftArrowIconProps) => (
  <svg
    xmlns="http://www.w3.org/2000/svg"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    className={className}
    {...props}
  >
    <path d="M19 12H5" />
    <path d="M12 19l-7-7 7-7" />
  </svg>
);