import clsx from "clsx";

interface SearchIconProps extends React.SVGProps<SVGSVGElement> {
  hasQuery?: boolean;
  isFocused?: boolean;
}

export const SearchIcon = ({
  hasQuery,
  isFocused,
  className,
  ...props
}: SearchIconProps) => {
  return (
    <svg
      className={clsx(
        "absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 transition-colors duration-200",
        hasQuery ? 
          isFocused ? "text-green-400" : "text-gray-200"
          : "text-gray-500",
        className
      )}
      fill="none"
      viewBox="0 0 24 24"
      stroke="currentColor"
      {...props}
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth={2}
        d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
      />
    </svg>
  );
};