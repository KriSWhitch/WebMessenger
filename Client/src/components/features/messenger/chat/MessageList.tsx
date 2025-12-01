'use client';

import React, { useEffect, useLayoutEffect, useRef } from 'react';
import { MessageBubble, MessageVM } from './MessageBubble';

const FAR_FROM_BOTTOM_EPSILON = 120;

export function MessageList(props: {
  messages: MessageVM[];
  externalContainerRef: React.RefObject<HTMLDivElement>;
  hasMore: boolean;
  loading: boolean;
  chatId?: string | number;
  onLoadMore: () => void;
}) {
  const { messages, externalContainerRef, hasMore, loading, chatId, onLoadMore } = props;
  const internalContainerRef = useRef<HTMLDivElement | null>(null);
  const containerRef = externalContainerRef ?? internalContainerRef;

  const bottomRef = useRef<HTMLDivElement | null>(null);
  const prevLenRef = useRef<number>(messages.length);
  const didInitialScrollRef = useRef<boolean>(false);

  const wasAtBottomRef = useRef<boolean>(true);

  const getEl = () => containerRef.current;

  const distanceToBottom = () => {
    const el = getEl();
    if (!el) return 0;
    return el.scrollHeight - el.clientHeight - el.scrollTop;
  };

  const isFarFromBottom = () => distanceToBottom() > FAR_FROM_BOTTOM_EPSILON;

  const rAF2 = (cb: () => void) => requestAnimationFrame(() => requestAnimationFrame(cb));

  const scrollToBottom = (behavior: ScrollBehavior = 'auto') => {
    rAF2(() => {
      bottomRef.current?.scrollIntoView({ behavior, block: 'end' });
      wasAtBottomRef.current = true;
    });
  };

  const preserve = (() => {
    let prevScrollHeight = 0;
    let prevScrollTop = 0;
    return {
      before: () => {
        const el = getEl();
        if (!el) return;
        prevScrollHeight = el.scrollHeight;
        prevScrollTop = el.scrollTop;
      },
      after: () => {
        const el = getEl();
        if (!el) return;
        const delta = el.scrollHeight - prevScrollHeight;
        el.scrollTop = prevScrollTop + delta;
      },
    };
  })();

  useLayoutEffect(() => {
    scrollToBottom('auto');
    didInitialScrollRef.current = true;
    wasAtBottomRef.current = true;
  }, [chatId]);

  useEffect(() => {
    const prevLen = prevLenRef.current;
    if (prevLen === 0 && messages.length > 0) {
      setTimeout(() => scrollToBottom('auto'), 0);
    }
    prevLenRef.current = messages.length;
  }, [messages.length]);

  useEffect(() => {
    const el = getEl();
    if (!el) return;

    const prevLen = prevLenRef.current;
    const nextLen = messages.length;
    const appended = nextLen > prevLen;

    if (appended) {
      if (wasAtBottomRef.current || !isFarFromBottom()) {
        scrollToBottom('smooth');
      }
    } else if (nextLen > 0 && el.scrollTop === 0) {
      preserve.after();
    }

    prevLenRef.current = nextLen;
  }, [messages]);

  const onScroll = (e: React.UIEvent<HTMLDivElement>) => {
    const el = e.currentTarget;
    wasAtBottomRef.current = !isFarFromBottom();

    if (el.scrollTop <= 0 && hasMore && !loading) {
      preserve.before();
      onLoadMore?.();
    }
  };

  useEffect(() => {
    const el = getEl();
    if (!el) return;

    const mo = new MutationObserver(() => {
      if (wasAtBottomRef.current) {
        scrollToBottom('auto');
      }
    });
    mo.observe(el, { childList: true, subtree: true });

    return () => mo.disconnect();
  }, [chatId]);

  return (
    <div
      key={String(chatId)}
      ref={containerRef}
      onScroll={onScroll}
      role="log"
      aria-live="polite"
      aria-relevant="additions"
      className={[
        'h-full overflow-y-auto px-2 pt-2 space-y-2',
        '[-webkit-overflow-scrolling:touch]',
        '[scrollbar-width:thin]',
        '[scrollbar-color:rgba(156,163,175,.6)_transparent]',
        '[&::-webkit-scrollbar]:w-2',
        '[&::-webkit-scrollbar]:h-2',
        '[&::-webkit-scrollbar-track]:bg-transparent',
        '[&::-webkit-scrollbar-thumb]:rounded-full',
        '[&::-webkit-scrollbar-thumb]:border-2',
        '[&::-webkit-scrollbar-thumb]:border-transparent',
        '[&::-webkit-scrollbar-thumb]:bg-clip-padding',
        '[&::-webkit-scrollbar-thumb]:bg-gray-400/60',
        '[&::-webkit-scrollbar-thumb]:transition-colors',
        'hover:[&::-webkit-scrollbar-thumb]:bg-gray-300/80',
      ].join(' ')}
    >
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
        messages.map((m) => <MessageBubble key={m.id} m={m} />)
      )}

      <div ref={bottomRef} />
    </div>
  );
}
