'use client';

import clsx from 'clsx';
import Image from 'next/image';

type AvatarProps = {
  src?: string;
  name?: string;
  className?: string;
  size?: number;
};

export const Avatar = ({ src, name = '', className, size }: AvatarProps) => {
  const sizeStyle = size ? { width: `${size}px`, height: `${size}px` } : undefined;
  
  return (
    <div
      style={sizeStyle}
      className={clsx(
        'relative rounded-full bg-gray-600 overflow-hidden flex items-center justify-center',
        className
      )}
    >
      {src ? (
        <Image
          src={src}
          alt={name}
          fill
          unoptimized
          sizes="64px"
          className={clsx('block object-cover object-center select-none', 'h-full w-full')}
        />
      ) : (
        <span className="text-lg font-medium">{name?.trim()?.charAt(0)?.toUpperCase() ?? 'U'}</span>
      )}
    </div>
  );
};
