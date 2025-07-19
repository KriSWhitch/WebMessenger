interface BadgeProps {
  count: number;
}

export const Badge = ({ count }: BadgeProps) => (
  <span className="bg-green-600 text-white text-xs h-5 w-5 rounded-full flex items-center justify-center flex-shrink-0">
    {count}
  </span>
);