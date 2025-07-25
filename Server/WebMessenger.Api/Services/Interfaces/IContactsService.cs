using WebMessenger.Api.Models;

namespace WebMessenger.Api.Services.Interfaces
{
    public interface IContactsService
    {
        Task<AddContactResponse> AddContactAsync(Guid currentUserId, AddContactRequest request);
        Task<IEnumerable<ContactDto>> GetContactsAsync(Guid currentUserId, string query);
        bool IsContact(Guid currentUserId, Guid id);
        Task<bool> IsContactAsync(Guid currentUserId, Guid id);
    }
}
