import { ChatIconSecondary } from "@/components/icons/ChatIconSecondary";
import { Chat } from "@/types/chat";
import { EmptyState } from "../EmptyState/EmptyState";

interface MessengerMainAreaProps {
  hasChats: boolean;
  selectedChat?: Chat;
}

export const MessengerMainArea = ({ 
  hasChats, 
  selectedChat 
}: MessengerMainAreaProps) => (
  <div className="flex-1 flex flex-col hidden md:flex">
    <div className="flex-1 overflow-y-auto bg-gray-800/50">
      {selectedChat ? (
        <div className="h-full flex flex-col">
          {/* Реализация чата будет здесь */}
          <div className="p-4 border-b border-gray-700">
            <h2 className="text-xl font-medium">{selectedChat.name}</h2>
          </div>
          <div className="flex-1 overflow-y-auto p-4">
            {/* Сообщения будут здесь */}
          </div>
          <div className="p-4 border-t border-gray-700">
            {/* Форма отправки сообщения */}
          </div>
        </div>
      ) : (
        <EmptyState
          icon={<ChatIconSecondary />}
          title={hasChats ? "Select a chat" : "Welcome to your messenger"}
          description={
            hasChats 
              ? "Choose a conversation from the list to start messaging" 
              : "Get started by creating your first chat"
          }
        />
      )}
    </div>
  </div>
);