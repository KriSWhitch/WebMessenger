import clsx from "clsx";
import { EmptyState } from "@/components/features/messenger/EmptyState/EmptyState";
import { ChatList } from "@/components/features/messenger/ChatList/ChatList";
import { SearchIcon } from "@/components/icons/SearchIcon";
import { ChatIcon } from "@/components/icons/ChatIcon";
import { Button } from "@/components/ui/Button/Button";
import { InputField } from "@/components/ui/Input/Input";
import { BurgerMenu } from "@/components/navigation/dropdown/BurgerMenu";
import { Chat } from "@/types/chat";
import { logoutClient } from "@/lib/client-auth";
import { useCallback, useEffect, useState } from "react";
import { Contact, UserSearchResult } from "@/types";
import { SearchResults } from "@/components/features/messenger/SearchResults/SearchResults";
import { useDebounce } from "@/hooks/useDebounce";
import { ContactList } from "../ContactList/ContactList";
import { LeftArrowIcon } from "@/components/icons/LeftArrowIcon";

interface MessengerSidebarProps {
  searchQuery: string;
  setSearchQuery: (value: string) => void;
  isSearchFocused: boolean;
  setIsSearchFocused: (value: boolean) => void;
  chats: Chat[];
  selectedChatId: string | null;
  onSelectChat: (chatId: string) => void;
  onSearchUserSelect: (userId: string) => void;
  onAddContact: (userId: string) => Promise<void>;
  contacts: Contact[];
  selectedContactId: string | null;
  onSelectContact: (contactId: string) => void;
  onSettingsClick: () => void;
}

export const MessengerSidebar = ({
  searchQuery,
  setSearchQuery,
  isSearchFocused,
  setIsSearchFocused,
  chats,
  selectedChatId,
  onSelectChat,
  onSearchUserSelect,
  onAddContact,
  onSettingsClick
}: MessengerSidebarProps) => {
  const [searchContactResults, setSearchContactResults] = useState<Contact[]>([]);
  const [searchUserResults, setSearchUserResults] = useState<UserSearchResult[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [showContacts, setShowContacts] = useState(false);

  const debouncedSearchQuery = useDebounce(searchQuery, 1000);

  const searchUsers = useCallback(async (query: string, validateQuery:(query: string) => boolean = () => { return true }) => {
    const isQueryValid = validateQuery(query);

    if (!isQueryValid)
      return;

    setIsSearching(true);

    try {
      const response = await fetch(`/api/users?query=${encodeURIComponent(query)}`);

      if (response.ok) {
        const data = await response.json();
        setSearchUserResults(data);
      }
    } catch (error) {
      console.error('Search failed:', error);
    } finally {
      setIsSearching(false);
    }
  }, []);

  const searchContacts = useCallback(async (query: string, validateQuery:(query: string) => boolean = () => { return true }) => {
    const isQueryValid = validateQuery(query);

    if (!isQueryValid)
      return;

    setIsSearching(true);

    try {
      const response = await fetch(`/api/contacts?query=${encodeURIComponent(query)}`);

      if (response.ok) {
        const data = await response.json();
        setSearchContactResults(data);
      }
    } catch (error) {
      console.error('Search failed:', error);
    } finally {
      setIsSearching(false);
    }
  }, []);

  useEffect(() => {
    setSearchQuery("");

    if (showContacts) {
      searchContacts("");
    }
  }, [showContacts])

  useEffect(() => {
    if (debouncedSearchQuery) {
      if (showContacts) {
        searchContacts(debouncedSearchQuery, (debouncedSearchQuery) => {
          if (debouncedSearchQuery.length < 3) 
            return false;
          return true;
        });
      } else {
        searchUsers(debouncedSearchQuery, (debouncedSearchQuery) => {
          if (debouncedSearchQuery.length < 3) 
            return false;
          return true;
        });
      }
    } else if (debouncedSearchQuery.length == 0) {
      if (showContacts) {
        searchContacts("");
      }
    }
  }, [debouncedSearchQuery]);
  
  return (
    <div className="w-full md:w-80 lg:w-96 flex-shrink-0 border-r border-gray-700 flex flex-col">
      <div className="p-3 border-b border-gray-700 relative">
        <div className="flex items-center gap-2">
          {showContacts ? (
            <Button 
              useBaseClasses={false}
              onClick={() => setShowContacts(false)}
              className="p-2 h-fit w-fit rounded-full hover:bg-gray-700 transition-colors"
            >
              <LeftArrowIcon className="h-5 w-5 text-white" />
            </Button>
          ) : (
            <BurgerMenu 
              className="ml-2 p-2 h-fit w-fit rounded-full hover:bg-gray-700 transition-colors"
              menuItems={[
                { label: 'Contact List', onClick: () => setShowContacts(true) },
                { label: 'Settings', onClick: () => onSettingsClick() },
                { 
                  label: 'Logout', 
                  onClick: async () => {
                    await logoutClient();
                  },
                  danger: true 
                }
              ]}
            />
          )}

          <div className="flex-1 min-w-0 relative">
            <InputField
              type="text"
              placeholder={
                showContacts 
                  ? "Search contacts" 
                  : "Search users or chats"
              }
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onFocus={() => setIsSearchFocused(true)}
              onBlur={() => setIsSearchFocused(false)}
              containerClass="mb-0"
              showError={false}
              useBaseClasses={false}
              className={clsx(
                "w-full pl-10 pr-4 py-2 bg-gray-800 rounded-lg border",
                "text-gray-200 placeholder-gray-500 transition-all duration-200",
                "focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent",
                "hover:border-gray-500 border-gray-700"
              )}
              icon={
                <SearchIcon 
                  hasQuery={!!searchQuery} 
                  isFocused={isSearchFocused} 
                />
              }
            />
          </div>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto">
        {searchQuery ? (
          <>
            {isSearching ? (
              <div className="p-4 text-center text-gray-400">Searching...</div>
            ) : (showContacts && searchQuery.length >= 3) ? (<>
              <ContactList 
                contacts={searchContactResults}
                onSelectContact={onSearchUserSelect}
              />
            </>) 
            : ((searchUserResults.length > 0 && searchQuery.length >= 3) ? (
              <SearchResults 
                results={searchUserResults}
                onSelectUser={onSearchUserSelect}
                onAddContact={onAddContact}
              />
            ) : (
              <EmptyState
                title="No users found"
                description={searchQuery.length < 3 ? 
                  "Enter at least 3 characters to search" : 
                  "Try a different search query"}
              />
            ))}
          </>
        ) : (showContacts && searchContactResults.length > 0) ? 
          <ContactList 
            contacts={searchContactResults}
            onSelectContact={onSearchUserSelect}
          /> : chats.length > 0 ? (
          <ChatList 
            chats={chats} 
            selectedChatId={selectedChatId}
            onSelectChat={onSelectChat}
          />
          ) : (
          <EmptyState
            icon={<ChatIcon className={"animate-bounce"} />}
            title="You don't have any chats yet"
            description="Start communicating now by adding new contacts"
          />
        )}
      </div>
    </div>
  )
};