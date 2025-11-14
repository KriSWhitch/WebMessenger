// components/ui/Avatar/Avatar.tsx
'use client';

import { useState } from 'react';
import clsx from 'clsx';

type AvatarProps = {
  src?: string;
  name?: string;
  className?: string;
  size?: number;
};

export const Avatar = ({ src, name = '', className, size = 44 }: AvatarProps) => {
  const [fitByHeight, setFitByHeight] = useState<boolean>(false);

  return (
    <div
      className={clsx(
        'relative rounded-full bg-gray-600 overflow-hidden flex items-center justify-center',
        className
      )}
      style={{
        width: size,
        height: size,
        aspectRatio: '1 / 1',
      }}
    >
      {src ? (
        <img
          src={src}
          alt={name}
          className={clsx(
            'block object-cover object-center select-none',
            fitByHeight ? 'h-full w-auto' : 'w-full h-auto'
          )}
          onLoad={(e) => {
            const img = e.currentTarget;
            const { naturalWidth, naturalHeight } = img;
            setFitByHeight(naturalHeight >= naturalWidth);
          }}
          decoding="async"
          loading="lazy"
        />
      ) : (
        <span className="text-lg font-medium">
          {name?.trim()?.charAt(0)?.toUpperCase() ?? 'U'}
        </span>
      )}
    </div>
  );
};