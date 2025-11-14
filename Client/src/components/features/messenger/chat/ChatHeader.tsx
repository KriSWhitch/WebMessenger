'use client';

import { LeftArrowIcon } from '@/components/icons/LeftArrowIcon';
import { Avatar } from '@/components/ui/Avatar/Avatar';

type Props = {
  peerUserId: string;
  username?: string | null;
  avatarUrl?: string | null;
  onOpenProfile?: () => void;
  onBack?: () => void;
  showBackButton?: boolean;
};

export function ChatHeader({
  peerUserId,
  username,
  avatarUrl,
  onOpenProfile,
  onBack,
  showBackButton,
}: Props) {
  const title = username ?? peerUserId;

  return (
    <div className="flex items-center justify-between px-4 py-2 border-b border-gray-700 bg-gray-900">
      <div className="flex items-center gap-3">
        {(showBackButton ?? false) && (
          <button
            onClick={onBack}
            aria-label="Back"
            className="mr-1 p-2 rounded-full hover:bg-gray-800 transition-colors md:hidden"
          >
            <LeftArrowIcon className="w-5 h-5 text-gray-200" />
          </button>
        )}

        <button
          className="flex items-center gap-3"
          onClick={onOpenProfile}
          aria-label="Open profile"
        >
          <div className="h-9 w-9 rounded-full overflow-hidden bg-gray-700">
            {avatarUrl ? <Avatar src={avatarUrl} size={36} /> : null}
          </div>
          <div className="text-left">
            <div className="text-sm font-semibold leading-tight">{title}</div>
          </div>
        </button>
      </div>

      <div className="flex items-center gap-2" />
    </div>
  );
}
