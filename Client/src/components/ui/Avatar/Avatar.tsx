import { clsx } from 'clsx';

interface AvatarProps {
  src?: string;
  name: string;
  className?: string;
}

export const Avatar = ({ src, name, className }: AvatarProps) => (
  <div className={clsx(
    "h-11 w-11 rounded-full bg-gray-600 flex-shrink-0 flex items-center justify-center overflow-hidden",
    className
  )}>
    {src ? (
      <img
        src={src}
        alt={name}
        className="h-full w-full object-cover"
      />
    ) : (
      <span className="text-lg font-medium">
        {name.charAt(0).toUpperCase()}
      </span>
    )}
  </div>
);