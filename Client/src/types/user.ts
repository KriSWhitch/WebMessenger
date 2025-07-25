export interface User {
  id: string;
  username: string;
  email: string;
  phoneNumber: string;
  firstName: string;
  lastName: string;
  bio?: string;
  avatarUrl?: string;
  isOnline: boolean;
  lastSeenAt: string;
  createdAt: string;
  lastLoginAt?: string;
}

export interface UserStatus {
  userId: string;
  isOnline: boolean;
  lastSeenAt?: string;
}

export interface UserSearchResult {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
  avatarUrl?: string;
  isOnline: boolean;
  isContact: boolean;
}