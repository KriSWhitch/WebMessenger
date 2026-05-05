using WebMessenger.Contracts.Models;

namespace WebMessenger.Api.Hubs.Interfaces
{
    public interface IChatClient
    {
        Task MessageCreated(MessageCreatedPayload payload, CancellationToken ct = default);
        Task ReadReceipt(ReadReceiptPayload payload, CancellationToken ct = default);
        Task Typing(TypingPayload payload, CancellationToken ct = default);
    }
}
