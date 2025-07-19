'use client';
import { MessengerMainArea } from "@/components/features/messenger/layout/MessengerMainArea";
import { MessengerSidebar } from "@/components/features/messenger/layout/MessengerSidebar";
import { Chat } from "@/types/chat";
import { useState } from "react";

export default function MessengerPage() {
  const [chats, setChats] = useState<Chat[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isSearchFocused, setIsSearchFocused] = useState(false);
  const [selectedChat, setSelectedChat] = useState<string | null>(null);

  const createNewChat = () => {
    const newChat: Chat = {
      id: Math.random().toString(36).substring(7),
      name: `New Chat ${chats.length + 1}`,
      isGroup: false,
      createdAt: new Date().toISOString(),
      lastMessage: {
        id: 'temp',
        content: 'Start chatting now!',
        senderId: 'current-user',
        chatId: 'temp-chat',
        sentAt: new Date().toISOString(),
        isRead: false,
      },
      unreadCount: 0,
      avatarUrl: ''
    };
    setChats([...chats, newChat]);
    setSelectedChat(newChat.id);
  };

  return (
    <div className="flex h-screen bg-gray-900 text-gray-200 overflow-hidden">
      <MessengerSidebar
        isMenuOpen={isMenuOpen}
        toggleMenu={() => setIsMenuOpen(!isMenuOpen)}
        searchQuery={searchQuery}
        setSearchQuery={setSearchQuery}
        isSearchFocused={isSearchFocused}
        setIsSearchFocused={setIsSearchFocused}
        chats={chats}
        onCreateNewChat={createNewChat}
        onSelectChat={setSelectedChat}
        selectedChatId={selectedChat}
      />
      
      <MessengerMainArea 
        hasChats={chats.length > 0} 
        selectedChat={chats.find(chat => chat.id === selectedChat)}
      />
    </div>
  );
}