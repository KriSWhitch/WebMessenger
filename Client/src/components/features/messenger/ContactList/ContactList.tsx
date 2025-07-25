// components/features/messenger/ContactList/ContactList.tsx
import { Contact } from "@/types/contact";
import { Avatar } from "@/components/ui/Avatar/Avatar";
import clsx from "clsx";
import { EmptyState } from "@/components/features/messenger/EmptyState/EmptyState";
import { ChatIcon } from "@/components/icons/ChatIcon";

interface ContactListProps {
  contacts: Contact[];
  onSelectContact: (contactId: string) => void;
}

export const ContactList = ({ 
  contacts, 
  onSelectContact
}: ContactListProps) => {
  if (!contacts || contacts.length === 0) {
    return (
      <EmptyState
        icon={<ChatIcon className="text-gray-400" />}
        title="You don't have any contacts yet"
        description="Start by searching and adding new contacts"
      />
    );
  }

  return (
    <div className="divide-y divide-gray-700">
      {contacts.map((contact) => (
        <div
          key={contact.id}
          onClick={() => onSelectContact(contact.userId)}
          className={clsx(
            "block hover:bg-gray-800/50 transition-colors duration-150 cursor-pointer"
          )}
        >
          <div className="p-3">
            <div className="flex items-center gap-3">
              <Avatar 
                src={contact.avatarUrl} 
                name={contact?.contactUser?.username || ""} 
                className="h-11 w-11"
              />
              <div className="flex-1 min-w-0">
                <div className="flex justify-between items-center">
                  <h3 className="text-sm font-medium truncate">
                    {contact.nickname}
                  </h3>
                  {contact.isOnline ? (
                    <span className="text-xs text-green-500">Online</span>
                  ) : (
                    <span className="text-xs text-gray-400">Offline</span>
                  )}
                </div>
                <p className="text-sm text-gray-400 truncate">
                  @{contact?.contactUser?.username || ""}
                </p>
              </div>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
};