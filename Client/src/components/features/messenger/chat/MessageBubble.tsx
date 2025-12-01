'use client';
import clsx from 'clsx';

export type MessageVM = {
  id: string;
  chatId: string;
  senderId: string;
  content: string;
  sentAt: string; // ISO
  editedAt?: string | null;
  isRead: boolean;
  _pending?: boolean;
  _failed?: boolean;
  _mine?: boolean;
};

export function MessageBubble({ m }: { m: MessageVM }) {
  const mine = !!m._mine;
  const container = clsx('max-w-[95%] md:max-w-[80%] break-words');
  const bubble = clsx(
    'inline-block rounded-2xl px-3 py-2 shadow-sm',
    mine ? 'bg-green-600 text-white' : 'bg-gray-900 border border-gray-700 text-gray-100'
  );

  return (
    <div className={container}>
      <div className={bubble}>
        <div className="whitespace-pre-wrap">{m.content}</div>
        <div
          className={clsx(
            'mt-1 text-[11px] flex items-center gap-2',
            mine ? 'text-white/70' : 'text-gray-400'
          )}
        >
          <span>{new Date(m.sentAt).toLocaleTimeString()}</span>
          {m._pending && <span className="italic">sending…</span>}
          {m._failed && <span className="text-red-400">failed</span>}
          {m.editedAt && <span className="italic">edited</span>}
        </div>
      </div>
    </div>
  );
}
