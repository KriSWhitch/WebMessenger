import { Badge } from '../../../ui/Badge/Badge';

interface ChatPreviewProps {
  name: string;
  lastMessage: string;
  time: string;
  unread?: number;
}

export const ChatPreview = ({ name, lastMessage, time, unread }: ChatPreviewProps) => (
  <div className="flex-1 min-w-0">
    <div className="flex justify-between items-center">
      <h3 className="text-sm font-medium truncate">{name}</h3>
      <span className="text-xs text-gray-400">{time}</span>
    </div>
    <div className="flex justify-between items-center gap-2">
      <p className="text-sm text-gray-400 truncate">{lastMessage}</p>
      {(unread && unread > 0) ? <Badge count={unread} /> : <></>}
    </div>
  </div>
);