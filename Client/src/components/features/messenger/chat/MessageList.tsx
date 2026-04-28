'use client';

import React, { useCallback, useEffect, useLayoutEffect, useRef } from 'react';
import { MessageBubble, MessageVM } from './MessageBubble';

const FAR_FROM_BOTTOM_EPSILON = 120;

export function MessageList(props: {
  messages: MessageVM[];
  externalContainerRef: React.RefObject<HTMLDivElement | null>;
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
  const prevScrollHeightRef = useRef<number>(0);
  const prevScrollTopRef = useRef<number>(0);

  const wasAtBottomRef = useRef<boolean>(true);

  const getEl = useCallback(() => containerRef.current, [containerRef]);

  const distanceToBottom = useCallback(() => {
    const el = getEl();
    if (!el) return 0;
    return el.scrollHeight - el.clientHeight - el.scrollTop;
  }, [getEl]);

  const isFarFromBottom = useCallback(
    () => distanceToBottom() > FAR_FROM_BOTTOM_EPSILON,
    [distanceToBottom]
  );

  const rAF2 = useCallback(
    (cb: () => void) => requestAnimationFrame(() => requestAnimationFrame(cb)),
    []
  );

  const scrollToBottom = useCallback((behavior: ScrollBehavior = 'auto') => {
    rAF2(() => {
      bottomRef.current?.scrollIntoView({ behavior, block: 'end' });
      wasAtBottomRef.current = true;
    });
  }, [rAF2]);

  const preserveBefore = useCallback(() => {
    const el = getEl();
    if (!el) return;
    prevScrollHeightRef.current = el.scrollHeight;
    prevScrollTopRef.current = el.scrollTop;
  }, [getEl]);

  const preserveAfter = useCallback(() => {
    const el = getEl();
    if (!el) return;
    const delta = el.scrollHeight - prevScrollHeightRef.current;
    el.scrollTop = prevScrollTopRef.current + delta;
  }, [getEl]);

  useLayoutEffect(() => {
    scrollToBottom('auto');
    didInitialScrollRef.current = true;
    wasAtBottomRef.current = true;
  }, [chatId, scrollToBottom]);

  useEffect(() => {
    const prevLen = prevLenRef.current;
    if (prevLen === 0 && messages.length > 0) {
      setTimeout(() => scrollToBottom('auto'), 0);
    }
    prevLenRef.current = messages.length;
  }, [messages.length, scrollToBottom]);

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
      preserveAfter();
    }

    prevLenRef.current = nextLen;
  }, [getEl, isFarFromBottom, messages, preserveAfter, scrollToBottom]);

  const onScroll = (e: React.UIEvent<HTMLDivElement>) => {
    const el = e.currentTarget;
    wasAtBottomRef.current = !isFarFromBottom();

    if (el.scrollTop <= 0 && hasMore && !loading) {
      preserveBefore();
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
  }, [chatId, getEl, scrollToBottom]);

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
