namespace WebMessenger.Api.Models
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public bool HasMore { get; init; }
        public DateTime? NextBefore { get; init; }
    }
}
