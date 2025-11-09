'use client';

import { MessengerMainArea } from "@/components/features/messenger/layout/MessengerMainArea";
import { MessengerSidebar } from "@/components/features/messenger/layout/MessengerSidebar";
import { UserSettings } from '@/components/features/messenger/UserSettings/UserSettings';
import { Chat } from "@/types/chat";
import { useCallback, useMemo, useState } from "react";
import type { DirectChatHeaderDto } from "@/types";
import clsx from "clsx";
import { UserProfilePanel } from "@/components/features/messenger/chat/UserProfilePanel";

export default function MessengerPage() {
  const [chats, setChats] = useState<Chat[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [isSearchFocused, setIsSearchFocused] = useState(false);
  const [selectedChat, setSelectedChat] = useState<string | null>(null);
  const [showSettings, setShowSettings] = useState(false);

  const [isProfileOpen, setIsProfileOpen] = useState(false);
  const [profileUserId, setProfileUserId] = useState<string | null>(null);

  const openDirectChatWithUser = useCallback(async (userId: string): Promise<Chat> => {
    const existing = chats.find(c => c.peerUserId === userId);
    if (existing) return existing;

    const headerResp = await fetch(`/api/chats/direct/${userId}/header`, {
      method: 'GET',
      cache: 'no-store',
      credentials: 'include',
    });
    if (!headerResp.ok) {
      throw new Error(await headerResp.text());
    }
    const header = (await headerResp.json()) as DirectChatHeaderDto;

    const newChat: Chat = {
      id: `dm-${userId}`,
      name: header.username ?? userId,
      isGroup: false,
      createdAt: new Date().toISOString(),
      lastMessage: undefined,
      unreadCount: 0,
      avatarUrl: header.avatarUrl ?? undefined,
      members: [{ userId }, { userId: 'current-user' }],
      serverChatId: header.chatId ?? null,
      peerUserId: userId,
    };

    setChats(prev => [...prev, newChat]);
    return newChat;
  }, [chats]);

  const handleUserSelect = useCallback(async (userId: string) => {
    try {
      const chat = await openDirectChatWithUser(userId);
      setSelectedChat(chat.id);
      setSearchQuery('');
      setShowSettings(false);
    } catch (e) {
      console.error('Failed to open direct chat:', e);
    }
  }, [openDirectChatWithUser]);

  const handleAddContact = useCallback(async (userId: string) => {
    try {
      const resp = await fetch(`/api/contacts/add`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ contactUserId: userId })
      });
      if (!resp.ok) {
        console.error('Failed to add contact:', await resp.text());
      }
    } catch (error) {
      console.error('Failed to add contact:', error);
    }
  }, []);

  const selectedChatObj = useMemo(
    () => chats.find(chat => chat.id === selectedChat),
    [chats, selectedChat]
  );

  const openProfilePanel = useCallback((userId: string) => {
    setProfileUserId(userId);
    setIsProfileOpen(true);
  }, []);

  const closeProfilePanel = useCallback(() => {
    setIsProfileOpen(false);
  }, []);

  const handleCloseChat = useCallback(() => {
    setIsProfileOpen(false);
    setShowSettings(false);
    setSelectedChat(null);
  }, []);

  return (
    <div className="relative flex h-screen bg-gray-900 text-gray-200 overflow-hidden">
      {!showSettings ? (
        <div
          className={clsx(
            "flex-shrink-0 border-r border-gray-700 flex flex-col z-[20]",
            "w-full md:w-80 lg:w-96",
            selectedChat ? "hidden md:flex" : "flex"
          )}
        >
          <MessengerSidebar
            searchQuery={searchQuery}
            setSearchQuery={setSearchQuery}
            isSearchFocused={isSearchFocused}
            setIsSearchFocused={setIsSearchFocused}
            chats={chats}
            onSelectChat={setSelectedChat}
            selectedChatId={selectedChat}
            onSearchUserSelect={handleUserSelect}
            onAddContact={handleAddContact}
            contacts={[]}
            selectedContactId={null}
            onSelectContact={handleUserSelect}
            onSettingsClick={() => setShowSettings(true)}
          />
        </div>
      ) : (
        <div className="w-full md:w-80 lg:w-96 flex-shrink-0 border-r border-gray-700 flex flex-col z-[40]">
          <UserSettings onClose={() => setShowSettings(false)} />
        </div>
      )}

      <MessengerMainArea
        hasChats={chats.length > 0}
        selectedChat={selectedChatObj}
        onOpenProfile={openProfilePanel}
        onCloseChat={handleCloseChat}
      />

      {profileUserId && (
        <UserProfilePanel
          userId={profileUserId}
          open={isProfileOpen}
          onClose={closeProfilePanel}
        />
      )}
    </div>
  );
}