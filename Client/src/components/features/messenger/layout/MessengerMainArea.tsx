'use client';

import { Chat } from "@/types/chat";
import { EmptyState } from "../EmptyState/EmptyState";
import { ChatIconSecondary } from "@/components/icons/ChatIconSecondary";
import { ChatHeader } from "../chat/ChatHeader";

interface MessengerMainAreaProps {
  hasChats: boolean;
  selectedChat?: Chat;
  onOpenProfile?: (userId: string) => void;
  onCloseChat?: () => void;
}

export const MessengerMainArea = ({
  hasChats,
  selectedChat,
  onOpenProfile,
  onCloseChat,
}: MessengerMainAreaProps) => {
  if (!selectedChat) {
    return (
      <div className="flex-1 flex flex-col relative z-[30]">
        <div className="flex-1 overflow-y-auto bg-gray-800/50">
          <EmptyState
            icon={<ChatIconSecondary />}
            title={hasChats ? "Select a chat" : "Welcome to your messenger"}
            description={
              hasChats
                ? "Choose a conversation from the list to start messaging"
                : "Get started by creating your first chat"
            }
          />
        </div>
      </div>
    );
  }

  const peerUserId =
    selectedChat.peerUserId ??
    selectedChat.members?.find(m => m.userId !== 'current-user')?.userId ??
    '';

  return (
    <div className="flex-1 flex flex-col relative z-[30]">
      <ChatHeader
        peerUserId={peerUserId}
        username={selectedChat.name}
        avatarUrl={selectedChat.avatarUrl}
        onOpenProfile={() => onOpenProfile?.(peerUserId)}
        onBack={onCloseChat}
        showBackButton={true}
      />

      <div className="flex-1 overflow-y-auto bg-gray-800/50">
        <div className="h-full flex items-center justify-center text-gray-400 text-sm px-4">
          Messages will be here
        </div>
      </div>

      <div className="p-4 border-t border-gray-700 bg-gray-900/60 backdrop-blur">
        <div className="bg-gray-900 border border-gray-700 rounded-2xl px-3 py-2 text-sm text-gray-400">
          Enter a message... (not currently active)
        </div>
      </div>
    </div>
  );
};