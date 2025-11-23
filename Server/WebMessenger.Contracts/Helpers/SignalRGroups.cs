namespace WebMessenger.Contracts.Helpers
{
    public static class SignalRGroups
    {
        public static string User(Guid userId) => $"user:{userId}";
        public static string Chat(Guid chatId) => $"chat:{chatId}";
        public static string Direct(Guid a, Guid b)
        {
            var (x, y) = a.CompareTo(b) <= 0 ? (a, b) : (b, a);
            return $"dm:{x}:{y}";
        }
    }

}
