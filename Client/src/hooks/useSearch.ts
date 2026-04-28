'use client';

import { useCallback, useEffect, useState } from 'react';
import type { Contact, UserSearchResult } from '@/types';
import { useDebounce } from '@/hooks/useDebounce';

export function useSearch(params: {
  showContacts: boolean;
  searchQuery: string;
  setSearchQuery: (value: string) => void;
  onAddContact: (userId: string) => Promise<void>;
}) {
  const { showContacts, searchQuery, setSearchQuery, onAddContact } = params;

  const [searchContactResults, setSearchContactResults] = useState<Contact[]>([]);
  const [searchUserResults, setSearchUserResults] = useState<UserSearchResult[]>([]);
  const [isSearching, setIsSearching] = useState(false);

  const debouncedSearchQuery = useDebounce(searchQuery, 1000);

  const onAddSearchUserToContact = useCallback(
    async (userId: string) => {
      await onAddContact(userId);
      setSearchUserResults((prev) =>
        prev.map((u) => (u.id === userId ? { ...u, isContact: true } : u))
      );
    },
    [onAddContact]
  );

  const searchUsers = useCallback(
    async (query: string, validateQuery: (value: string) => boolean = () => true) => {
      if (!validateQuery(query)) return;
      setIsSearching(true);
      try {
        const response = await fetch(`/api/users?query=${encodeURIComponent(query)}`);
        if (response.ok) {
          const data = (await response.json()) as UserSearchResult[];
          setSearchUserResults(data);
        }
      } catch (error) {
        console.error('Search failed:', error);
      } finally {
        setIsSearching(false);
      }
    },
    []
  );

  const searchContacts = useCallback(
    async (query: string, validateQuery: (value: string) => boolean = () => true) => {
      if (!validateQuery(query)) return;
      setIsSearching(true);
      try {
        const response = await fetch(`/api/contacts?query=${encodeURIComponent(query)}`);
        if (response.ok) {
          const data = (await response.json()) as Contact[];
          setSearchContactResults(data);
        }
      } catch (error) {
        console.error('Search failed:', error);
      } finally {
        setIsSearching(false);
      }
    },
    []
  );

  useEffect(() => {
    setSearchQuery('');
    if (showContacts) {
      void searchContacts('');
    }
  }, [showContacts, searchContacts, setSearchQuery]);

  useEffect(() => {
    if (debouncedSearchQuery) {
      if (showContacts) {
        void searchContacts(debouncedSearchQuery, (value) => value.length >= 3);
      } else {
        void searchUsers(debouncedSearchQuery, (value) => value.length >= 3);
      }
      return;
    }

    if (debouncedSearchQuery.length === 0 && showContacts) {
      void searchContacts('');
    }
  }, [debouncedSearchQuery, showContacts, searchContacts, searchUsers]);

  return {
    searchContactResults,
    searchUserResults,
    isSearching,
    onAddSearchUserToContact,
  };
}
