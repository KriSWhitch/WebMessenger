import { Chat } from "@/types/chat";
import { Avatar } from "@/components/ui/Avatar/Avatar";
import clsx from "clsx";
import { Badge } from "@/components/ui/Badge/Badge";

interface ChatListItemProps {
  chat: Chat;
  isSelected: boolean;
  onSelect: () => void;
}

export const ChatListItem = ({ 
  chat, 
  isSelected, 
  onSelect 
}: ChatListItemProps) => {
  const lastMessage = chat.lastMessage?.content || "No messages yet";
  const time = new Date(chat.lastMessage?.sentAt || chat.createdAt)
    .toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

  return (
    <div
      onClick={onSelect}
      className={clsx(
        "block hover:bg-gray-800/50 transition-colors duration-150 cursor-pointer",
        isSelected && "bg-gray-800/70"
      )}
    >
      <div className="p-3">
        <div className="flex items-center gap-3">
          <Avatar 
            src={chat.avatarUrl} 
            name={chat.name || "Chat"} 
            className="h-11 w-11"
          />
          <div className="flex-1 min-w-0">
            <div className="flex justify-between items-center">
              <h3 className="text-sm font-medium truncate">
                {chat.name || "New Chat"}
              </h3>
              <span className="text-xs text-gray-400">{time}</span>
            </div>
            <div className="flex justify-between items-center gap-2">
              <p className="text-sm text-gray-400 truncate">{lastMessage}</p>
              {(chat.unreadCount && chat.unreadCount > 0) ? (
                <Badge count={chat.unreadCount} />
              ) : <></>}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};