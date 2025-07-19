import { User } from './user';

export interface Chat {
  id: string;
  name?: string;
  avatarUrl?: string;
  isGroup: boolean;
  createdAt: string;
  lastMessage?: Message;
  unreadCount?: number;
  
  description?: string;
  creatorId?: string;
  membersCount?: number;
}

export interface ChatMember {
  id: string;
  userId: string;
  chatId: string;
  user: User;
  joinedAt: string;
  lastReadAt?: string;
  role?: 'member' | 'admin' | 'creator';
}

export interface ChatWithMembers extends Chat {
  members: ChatMember[];
}

export interface Message {
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
}

export interface Attachment {
  id: string;
  messageId: string;
  url: string;
  type: 'image' | 'video' | 'audio' | 'file';
  fileName?: string;
  fileSize?: number;
  width?: number;
  height?: number;
  duration?: number;
}

export interface Reaction {
  id: string;
  messageId: string;
  userId: string;
  emoji: string;
  createdAt: string;
  user?: User;
}

export interface CreateChatRequest {
  name?: string;
  isGroup: boolean;
  memberIds: string[];
  avatar?: File;
}

export interface SendMessageRequest {
  chatId: string;
  content: string;
  replyToId?: string;
  attachments?: File[];
}