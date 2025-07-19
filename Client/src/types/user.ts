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

export interface Contact {
  id: string;
  ownerUserId: string;
  contactUserId: string;
  contactUser: User;
  nickname?: string;
  addedAt: string;
}

export interface UserStatus {
  userId: string;
  isOnline: boolean;
  lastSeenAt?: string;
}