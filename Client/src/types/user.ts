export type User = {
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
};

export type UserProfileDto = {
  id: string;
  username?: string;
  email?: string;
  phoneNumber?: string;
  firstName?: string;
  lastName?: string;
  bio?: string;
  avatarUrl?: string;
  isOnline: boolean;
};

export type UpdateProfileDto = {
  email?: string;
  phoneNumber?: string;
  firstName?: string;
  lastName?: string;
  bio?: string;
  avatarUrl?: string;
};

export type UserStatus = {
  userId: string;
  isOnline: boolean;
  lastSeenAt?: string;
};

export type UserSearchResult = {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
  avatarUrl?: string;
  isOnline: boolean;
  isContact: boolean;
};