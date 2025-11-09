import { User } from './user';


export type Chat = {
  id: string;
  name: string;
  isGroup: boolean;
  createdAt: string;
  lastMessage?: {
    id: string;
    content: string;
    senderId: string;
    chatId: string;
    sentAt: string;
    isRead: boolean;
  };
  unreadCount: number;
  avatarUrl?: string;
  members?: { userId: string }[];

  serverChatId?: string | null;
  peerUserId?: string;
};


export type ChatMember = {
  id: string;
  userId: string;
  chatId: string;
  user: User;
  joinedAt: string;
  lastReadAt?: string;
  role?: 'member' | 'admin' | 'creator';
};

export interface ChatWithMembers extends Chat {
  members: ChatMember[];
};

export type Message = {
  id: string;
  content: string;
  senderId: string;
  chatId: string;
  sentAt: string;
  editedAt?: string;
  isRead: boolean;
  isEdited?: boolean;
  isDeleted?: boolean;
  
  sender?: User;
  chat?: Chat;
  
  attachments?: Attachment[];
  replyToMessage?: Message;
  reactions?: Reaction[];
};

export type Attachment = {
  id: string;
  messageId: string;
  url: string;
  type: 'image' | 'video' | 'audio' | 'file';
  fileName?: string;
  fileSize?: number;
  width?: number;
  height?: number;
  duration?: number;
};

export type Reaction = {
  id: string;
  messageId: string;
  userId: string;
  emoji: string;
  createdAt: string;
  user?: User;
};

export type CreateChatRequest = {
  name?: string;
  isGroup: boolean;
  memberIds: string[];
  avatar?: File;
};

export type SendMessageRequest = {
  chatId: string;
  content: string;
  replyToId?: string;
  attachments?: File[];
};

export type DirectChatHeaderDto = {
  otherUserId: string;
  username?: string | null;
  avatarUrl?: string | null;
  isOnline: boolean;
  chatId?: string | null;
};