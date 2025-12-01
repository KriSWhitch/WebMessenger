export function makeDmKey(meId: string, otherId: string): string {
  const [a, b] = meId?.localeCompare(otherId) <= 0 ? [meId, otherId] : [otherId, meId];
  return `dm:${a}:${b}`;
}
