'use client';
import { MessengerMainArea } from "@/components/features/messenger/layout/MessengerMainArea";
import { MessengerSidebar } from "@/components/features/messenger/layout/MessengerSidebar";
import { Chat } from "@/types/chat";
import { useState } from "react";

export default function MessengerPage() {
  const [chats, setChats] = useState<Chat[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
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

  const createChatWithUser = (userId: string) => {
    const newChat: Chat = {
      id: Math.random().toString(36).substring(7),
      name: `New Chat ${userId}`,
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

    return newChat;
  };

  const handleUserSelect = async (userId: string) => {
    const existingChat = chats.find(chat => 
      chat.members?.some(member => member.userId === userId)
    );
    
    if (existingChat) {
      setSelectedChat(existingChat.id);
    } else {
      const newChat = await createChatWithUser(userId);
      setChats([...chats, newChat]);
      setSelectedChat(newChat.id);
    }
    setSearchQuery('');
  };

  const handleAddContact = async (userId: string) => {
    try {
      const response = await fetch('/api/contacts/add', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ contactUserId: userId })
      });
      
      if (response.ok) {
        // const { chatId } = await response.json();
        // handleSearchUserSelect(userId);
      }
    } catch (error) {
      console.error('Failed to add contact:', error);
    }
  };



  return (
    <div className="flex h-screen bg-gray-900 text-gray-200 overflow-hidden">
      <MessengerSidebar
        searchQuery={searchQuery}
        setSearchQuery={setSearchQuery}
        isSearchFocused={isSearchFocused}
        setIsSearchFocused={setIsSearchFocused}
        chats={chats}
        onCreateNewChat={createNewChat}
        onSelectChat={setSelectedChat}
        selectedChatId={selectedChat}
        onSearchUserSelect={handleUserSelect}
        onAddContact={handleAddContact} contacts={[]} 
        selectedContactId={null} 
        onSelectContact={handleUserSelect}      
      />
      
      <MessengerMainArea 
        hasChats={chats.length > 0} 
        selectedChat={chats.find(chat => chat.id === selectedChat)}
      />
    </div>
  );
}