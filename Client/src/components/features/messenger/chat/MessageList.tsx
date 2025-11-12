'use client';
import { useEffect } from 'react';
import { MessageBubble, type MessageVM } from './MessageBubble';

export function MessageList(props: {
  messages: MessageVM[];
  containerRef: React.RefObject<HTMLDivElement>;
  hasMore: boolean;
  loading: boolean;
  onLoadMore: () => void;
}) {
  const { messages, containerRef, hasMore, loading, onLoadMore } = props;

  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;

    const onScroll = () => {
      if (el.scrollTop <= 120 && hasMore && !loading) {
        onLoadMore();
      }
    };

    el.addEventListener('scroll', onScroll, { passive: true });
    return () => el.removeEventListener('scroll', onScroll);
  }, [containerRef, hasMore, loading, onLoadMore]);

  return (
    <div ref={containerRef} className="h-full overflow-y-auto p-3 md:p-4 space-y-3">
      {hasMore && (
        <div className="text-center text-gray-400 text-xs">
          {loading ? 'Loading…' : 'Scroll up to load previous'}
        </div>
      )}

      {messages.length === 0 && !loading ? (
        <div className="h-full flex items-center justify-center text-gray-400 text-sm px-4">
          No messages yet. Say hello 👋
        </div>
      ) : (
        messages.map(m => <MessageBubble key={m.id} m={m} />)
      )}
    </div>
  );
}