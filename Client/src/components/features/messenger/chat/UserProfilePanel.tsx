'use client';

import { useEffect, useState } from 'react';
import Image from 'next/image';
import type { UserProfileDto } from '@/types';
import clsx from 'clsx';
import { CloseIcon } from '@/components/icons/CloseIcon';

type Props = {
  userId: string;
  open: boolean;
  onClose: () => void;
};

export function UserProfilePanel({ userId, open, onClose }: Props) {
  const [data, setData] = useState<UserProfileDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setError(null);
    setData(null);
    fetch(`/api/users/profile/${userId}`, { cache: 'no-store', credentials: 'include' })
      .then(async (r) => {
        if (!r.ok) throw new Error(await r.text());
        return r.json() as Promise<UserProfileDto>;
      })
      .then(setData)
      .catch((e) => setError(e.message));
  }, [userId, open]);

  return (
    <>
      <div
        className={clsx(
          'fixed inset-0 bg-black/40 transition-opacity duration-200',
          open ? 'opacity-100 pointer-events-auto' : 'opacity-0 pointer-events-none',
          'z-[60]'
        )}
        onClick={onClose}
      />

      <aside
        className={clsx(
          'fixed top-0 right-0 h-dvh w-full md:w-[28rem] max-w-[100vw] bg-gray-900 border-l border-gray-700',
          'transform transition-transform duration-250 ease-out z-[61]',
          open ? 'translate-x-0' : 'translate-x-full'
        )}
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
      >
        <div className="flex items-center justify-between px-4 py-3 border-b border-gray-700">
          <h2 className="text-sm font-semibold">User Profile</h2>
          <button
            onClick={onClose}
            className="p-2 h-fit w-fit rounded-full hover:bg-gray-800 transition-colors text-gray-300"
            aria-label="Close"
            title="Close"
          >
            <CloseIcon className="w-6 h-6 text-white" />
          </button>
        </div>

        <div className="p-4 w-full mx-auto">
          <div className="flex flex-col items-center mb-8">
            <div className="relative mb-4">
              {data?.avatarUrl ? (
                <Image
                  src={data.avatarUrl}
                  alt="avatar"
                  width={128}
                  height={128}
                  className="h-32 w-32 rounded-full object-cover"
                />
              ) : (
                <div className="h-32 w-32 rounded-full bg-gray-700" />
              )}
            </div>
            <h3 className="text-xl font-medium text-gray-200">@{data?.username ?? '—'}</h3>
          </div>

          <div className="space-y-6 px-2 md:px-4">
            <div className="space-y-1">
              <h3 className="text-lg font-medium text-gray-300">Personal Information</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 pt-2">
                <div>
                  <p className="text-sm text-gray-400">First Name</p>
                  <p className="truncate">{data?.firstName || '-'}</p>
                </div>
                <div>
                  <p className="text-sm text-gray-400">Last Name</p>
                  <p className="truncate">{data?.lastName || '-'}</p>
                </div>
              </div>
            </div>

            <div className="space-y-4">
              <div>
                <p className="text-sm text-gray-400">Email</p>
                <p className="truncate">{data?.email || '-'}</p>
              </div>
              <div>
                <p className="text-sm text-gray-400">Phone Number</p>
                <p className="truncate">{data?.phoneNumber || '-'}</p>
              </div>
              <div>
                <p className="text-sm text-gray-400">Bio</p>
                <p className="whitespace-pre-line break-words">
                  {data?.bio || 'No bio provided'}
                </p>
              </div>
            </div>

            {!!error && (
              <div className="text-xs text-red-400">Error loading profile: {error}</div>
            )}
          </div>
        </div>
      </aside>
    </>
  );
}