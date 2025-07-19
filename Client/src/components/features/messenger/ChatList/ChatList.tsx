import { Chat } from "@/types/chat";
import { ChatListItem } from "./ChatListItem";

interface ChatListProps {
  chats: Chat[];
  selectedChatId: string | null;
  onSelectChat: (chatId: string) => void;
}

export const ChatList = ({ 
  chats, 
  selectedChatId, 
  onSelectChat 
}: ChatListProps) => (
  <div className="divide-y divide-gray-700">
    {chats.map((chat) => (
      <ChatListItem
        key={chat.id}
        chat={chat}
        isSelected={chat.id === selectedChatId}
        onSelect={() => onSelectChat(chat.id)}
      />
    ))}
  </div>
);