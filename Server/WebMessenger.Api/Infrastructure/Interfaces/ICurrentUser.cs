namespace WebMessenger.Api.Infrastructure.Interfaces
{
    public interface ICurrentUser
    {
        bool IsAuthenticated { get; }
        Guid Id { get; }
        string? Username { get; }
    }

}
