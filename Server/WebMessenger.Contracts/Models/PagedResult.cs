namespace WebMessenger.Contracts.Models
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = [];
        public bool HasMore { get; init; }
        public DateTime? NextBefore { get; init; }
    }
}
