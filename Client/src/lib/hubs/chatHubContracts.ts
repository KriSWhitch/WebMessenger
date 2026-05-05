export const ChatHubEvents = {
  MessageCreated: 'MessageCreated',
  Typing: 'Typing',
  ReadReceipt: 'ReadReceipt',
} as const;

export const ChatHubMethods = {
  JoinChat: 'JoinChat',
  JoinDirect: 'JoinDirect',
  LeaveChat: 'LeaveChat',
  LeaveDirect: 'LeaveDirect',
  Typing: 'Typing',
  MarkRead: 'MarkRead',
} as const;
