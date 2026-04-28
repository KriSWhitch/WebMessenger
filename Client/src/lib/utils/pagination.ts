export type NormalizedPage<T> = {
  items: T[];
  hasMore: boolean;
  nextBefore: string | null;
};

type AnyPage<T> =
  | {
      items?: T[];
      hasMore?: boolean;
      nextBefore?: string | null;
    }
  | {
      data?: T[];
      hasMore?: boolean;
      nextBefore?: string | null;
    }
  | undefined
  | null;

export function normalizePage<T>(json: AnyPage<T>): NormalizedPage<T> {
  if (json && Array.isArray((json as { items?: T[] }).items)) {
    const typed = json as { items: T[]; hasMore?: boolean; nextBefore?: string | null };
    return {
      items: typed.items,
      hasMore: !!typed.hasMore,
      nextBefore: typed.nextBefore ?? null,
    };
  }

  const typed = (json ?? {}) as { data?: T[]; hasMore?: boolean; nextBefore?: string | null };
  return {
    items: Array.isArray(typed.data) ? typed.data : [],
    hasMore: !!typed.hasMore,
    nextBefore: typed.nextBefore ?? null,
  };
}

export function mergeUniqueById<T>(base: T[], incoming: T[], getId: (item: T) => string): T[] {
  const map = new Map<string, T>();
  for (const item of base) map.set(getId(item), item);
  for (const item of incoming) map.set(getId(item), item);
  return Array.from(map.values());
}

export function byDateDesc<T>(getDate: (item: T) => string): (a: T, b: T) => number {
  return (a, b) => new Date(getDate(b)).getTime() - new Date(getDate(a)).getTime();
}

export function byDateAsc<T>(getDate: (item: T) => string): (a: T, b: T) => number {
  return (a, b) => new Date(getDate(a)).getTime() - new Date(getDate(b)).getTime();
}
