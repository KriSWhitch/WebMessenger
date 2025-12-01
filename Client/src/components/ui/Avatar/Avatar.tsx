'use client';

import clsx from 'clsx';

type AvatarProps = {
  src?: string;
  name?: string;
  className?: string;
  size?: number;
};

export const Avatar = ({ src, name = '', className }: AvatarProps) => {
  return (
    <div
      className={clsx(
        'relative rounded-full bg-gray-600 overflow-hidden flex items-center justify-center',
        className
      )}
    >
      {src ? (
        <img
          src={src}
          alt={name}
          className={clsx('block object-cover object-center select-none', 'h-full w-full')}
          decoding="async"
          loading="lazy"
        />
      ) : (
        <span className="text-lg font-medium">{name?.trim()?.charAt(0)?.toUpperCase() ?? 'U'}</span>
      )}
    </div>
  );
};
