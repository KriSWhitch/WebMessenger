// components/features/messenger/SearchResults.tsx
import { UserSearchResult } from "@/types";
import { Avatar } from "@/components/ui/Avatar/Avatar";
import { Button } from "@/components/ui/Button/Button";
import { SendMessageIcon } from "@/components/icons/SendMessageIcon";
import { AddContactIcon } from "@/components/icons/AddContactIcon";

interface SearchResultsProps {
  results: UserSearchResult[];
  onSelectUser: (userId: string) => void;
  onAddContact: (userId: string) => Promise<void>;
}

export const SearchResults = ({ 
  results, 
  onSelectUser,
  onAddContact
}: SearchResultsProps) => (
  <div className="divide-y divide-gray-700">
    {results.map((user) => (
      <div 
        key={user.id} 
        className="p-3 hover:bg-gray-800/50 transition-colors duration-150"
      >
        <div className="flex items-center gap-3">
          <Avatar 
            src={user.avatarUrl} 
            name={`${user?.username}`} 
            className="h-11 w-11"
          />
          <div className="flex-1 min-w-0">
            <div className="flex justify-between items-center">
              <h3 className="text-sm font-medium truncate">
                {user.firstName} {user.lastName}
              </h3>
              {user.isOnline && (
                <span className="text-xs text-green-500">Online</span>
              )}
            </div>
            <p className="text-sm text-gray-400 truncate">@{user.username}</p>
          </div>
          {user.isContact ? (
            <Button
              variant="none"
              className="flex-end w-4 h-4 bg-transparent text-white hover:text-green-500"
              useBaseClasses={false}
              onClick={() => onSelectUser(user.id)}
            >
              <SendMessageIcon className="w-4 h-4" />
            </Button>
          ) : (
            <Button
              variant="none"
              className="flex-end w-4 h-4 text-white hover:text-green-500"
              useBaseClasses={false}
              onClick={() => onAddContact(user.id)}
            >
              <AddContactIcon className="w-4 h-4" />
            </Button>
          )}
        </div>
      </div>
    ))}
  </div>
);