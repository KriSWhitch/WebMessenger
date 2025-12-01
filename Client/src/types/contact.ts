import { User } from './user';

export type Contact = {
  id: string;
  userId: string;
  nickname?: string;
  avatarUrl?: string;
  isOnline: boolean;
  addedAt: Date;
  ownerUserId: string;
  contactUserId: string;
  contactUser?: User;
};
