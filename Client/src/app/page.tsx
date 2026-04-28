'use client';

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import clsx from 'clsx';
import { MessengerMainArea } from '@/components/features/messenger/layout/MessengerMainArea';
import { MessengerSidebar } from '@/components/features/messenger/layout/MessengerSidebar';
import { UserSettings } from '@/components/features/messenger/UserSettings/UserSettings';
import { UserProfilePanel } from '@/components/features/messenger/chat/UserProfilePanel';
import { useCurrentUser } from '@/hooks/useCurrentUser';
import { useChatListManagement } from '@/hooks/useChatListManagement';
import { useDirectChatResolution } from '@/hooks/useDirectChatResolution';

export default function MessengerPage() {
  const { currentUserId } = useCurrentUser();
  const [searchQuery, setSearchQuery] = useState('');
  const [isSearchFocused, setIsSearchFocused] = useState(false);
  const [showSettings, setShowSettings] = useState(false);
  const [isProfileOpen, setIsProfileOpen] = useState(false);
  const [selectedChat, setSelectedChat] = useState<string | null>(null);
  const [profileUserId, setProfileUserId] = useState<string | null>(null);

  const selectedServerChatIdRef = useRef<string | null>(null);
  const selectedPeerUserIdRef = useRef<string | null>(null);

  const { chats, setChats, hasMore, loading, loadMoreChats, onChatRead } = useChatListManagement({
    currentUserId,
    selectedServerChatId: selectedServerChatIdRef.current,
    selectedPeerUserId: selectedPeerUserIdRef.current,
    setSelectedChat,
  });

  const { openDirectChatWithUser, resolveChatSelection } = useDirectChatResolution({
    chats,
    setChats,
    currentUserId,
  });

  const handleUserSelect = useCallback(
    async (userId: string) => {
      try {
        const chat = await openDirectChatWithUser(userId);
        setSelectedChat(chat.id);
        setSearchQuery('');
        setShowSettings(false);
      } catch (e) {
        console.error('Failed to open direct chat:', e);
      }
    },
    [openDirectChatWithUser]
  );

  const handleChatSelect = useCallback(
    async (chatId: string) => {
      try {
        const resolvedId = await resolveChatSelection(chatId);
        if (!resolvedId) return;
        setSelectedChat(resolvedId);
        setShowSettings(false);
      } catch (e) {
        console.error('Failed to resolve chat by chatId:', e);
      }
    },
    [resolveChatSelection]
  );

  const handleAddContact = useCallback(async (userId: string) => {
    try {
      const resp = await fetch(`/api/contacts/add`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ contactUserId: userId }),
      });
      if (!resp.ok) console.error('Failed to add contact:', await resp.text());
    } catch (error) {
      console.error('Failed to add contact:', error);
    }
  }, []);

  const selectedChatObj = useMemo(
    () => chats.find((chat) => chat.id === selectedChat) ?? undefined,
    [chats, selectedChat]
  );

  useEffect(() => {
    const sid = selectedChatObj?.serverChatId ?? selectedChatObj?.id ?? null;
    selectedServerChatIdRef.current = sid;
    selectedPeerUserIdRef.current = selectedChatObj?.peerUserId ?? null;
  }, [selectedChatObj]);

  const openProfilePanel = useCallback((userId: string) => {
    setProfileUserId(userId);
    setIsProfileOpen(true);
  }, []);

  const closeProfilePanel = useCallback(() => setIsProfileOpen(false), []);

  const handleCloseChat = useCallback(() => {
    setIsProfileOpen(false);
    setShowSettings(false);
    setSelectedChat(null);
  }, []);

  return (
    <div className="relative flex h-screen bg-gray-900 text-gray-200 overflow-hidden">
      <div
        className={clsx(
          'relative flex-shrink-0 border-r border-gray-700 z-[20]',
          selectedChat && !showSettings ? 'w-0' : 'w-full',
          'md:w-80 lg:w-96',
          'transition-[width] duration-0'
        )}
      >
        <div className={clsx('h-full flex flex-col', selectedChat ? 'hidden md:flex' : 'flex')}>
          <MessengerSidebar
            searchQuery={searchQuery}
            setSearchQuery={setSearchQuery}
            isSearchFocused={isSearchFocused}
            setIsSearchFocused={setIsSearchFocused}
            chats={chats}
            onSelectChat={handleChatSelect}
            selectedChatId={selectedChat}
            onSearchUserSelect={handleUserSelect}
            onAddContact={handleAddContact}
            contacts={[]}
            selectedContactId={null}
            onSelectContact={handleUserSelect}
            onSettingsClick={() => setShowSettings(true)}
          />
          {hasMore && (
            <div className="p-3 border-t border-gray-800">
              <button
                className="w-full rounded bg-gray-800 px-3 py-2 text-sm hover:bg-gray-700 disabled:opacity-60"
                disabled={loading}
                onClick={loadMoreChats}
              >
                {loading ? 'Loading…' : 'Show more'}
              </button>
            </div>
          )}
        </div>

        <aside
          className={clsx(
            'absolute inset-0 z-[40]',
            'bg-gray-900 border-r border-gray-700',
            'will-change-transform transform transition-transform duration-300 ease-out',
            'transition-opacity duration-300',
            showSettings
              ? 'translate-x-0 opacity-100 pointer-events-auto'
              : '-translate-x-full opacity-0 pointer-events-none'
          )}
        >
          <UserSettings onClose={() => setShowSettings(false)} />
        </aside>
      </div>

      <MessengerMainArea
        hasChats={chats.length > 0}
        selectedChat={selectedChatObj}
        onOpenProfile={openProfilePanel}
        onCloseChat={handleCloseChat}
        onChatRead={onChatRead}
      />

      {profileUserId && (
        <UserProfilePanel userId={profileUserId} open={isProfileOpen} onClose={closeProfilePanel} />
      )}
    </div>
  );
}
