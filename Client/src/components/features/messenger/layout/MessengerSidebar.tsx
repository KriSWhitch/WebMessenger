import clsx from "clsx";
import router from "next/router";
import { EmptyState } from "@/components/features/messenger/EmptyState/EmptyState";
import { ChatList } from "@/components/features/messenger/ChatList/ChatList";
import { SearchIcon } from "@/components/icons/SearchIcon";
import { ChatIcon } from "@/components/icons/ChatIcon";
import { Button } from "@/components/ui/Button/Button";
import { InputField } from "@/components/ui/Input/Input";
import { BurgerMenu } from "@/components/navigation/dropdown/BurgerMenu";
import { Chat } from "@/types/chat";
import { logoutClient } from "@/lib/client-auth";

interface MessengerSidebarProps {
  searchQuery: string;
  setSearchQuery: (value: string) => void;
  isSearchFocused: boolean;
  setIsSearchFocused: (value: boolean) => void;
  chats: Chat[];
  selectedChatId: string | null;
  onCreateNewChat: () => void;
  onSelectChat: (chatId: string) => void;
}

const handleLogout = async () => {
  const { success, error } = await logoutClient();
  if (success) {
    router.push('/auth/login');
  } else {
    alert(`Logout failed: ${error}`);
  }
};

export const MessengerSidebar = ({
  searchQuery,
  setSearchQuery,
  isSearchFocused,
  setIsSearchFocused,
  chats,
  selectedChatId,
  onCreateNewChat,
  onSelectChat,
}: MessengerSidebarProps) => (
  <div className="w-full md:w-80 lg:w-96 flex-shrink-0 border-r border-gray-700 flex flex-col">
    <div className="p-3 border-b border-gray-700 relative">
      <div className="flex items-center gap-2">
        <BurgerMenu 
          className="ml-2"
          menuItems={[
            { label: 'Contact List', onClick: () => console.log('Contacts') },
            { label: 'Settings', onClick: () => console.log('Settings') },
            { 
              label: 'Logout', 
              onClick: async () => {
                await logoutClient();
              },
              danger: true 
            }
          ]}
        />

        <div className="flex-1 min-w-0 relative">
          <InputField
            type="text"
            placeholder="Search contacts or chats"
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
      {chats.length > 0 ? (
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
          action={
            <Button
              variant="primary"
              className="w-full max-w-xs transform hover:scale-105 transition-transform duration-150"
              onClick={onCreateNewChat}
            >
              Start New Chat
            </Button>
          }
        />
      )}
    </div>
  </div>
);