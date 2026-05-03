using Microsoft.EntityFrameworkCore;
using WebMessenger.Api.Projections.Contacts;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Contracts.Models;
using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;

namespace WebMessenger.Api.Services
{
    public class ContactsService(IUnitOfWork unitOfWork, ILogger<ContactsService> logger) : IContactsService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ILogger<ContactsService> _logger = logger;

        public async Task<AddContactResponse> AddContactAsync(Guid currentUserId, AddContactRequest request)
        {
            if (currentUserId == request.ContactUserId)
                throw new InvalidOperationException("Cannot add yourself as a contact");

            if (await IsContactAsync(currentUserId, request.ContactUserId))
                throw new InvalidOperationException("User is already in your contacts");

            var contact = new Contact
            {
                OwnerUserId = currentUserId,
                OwnerUser = null!,
                ContactUserId = request.ContactUserId,
                ContactUser = null!,
                Nickname = request.Nickname,
                AddedAt = DateTime.UtcNow
            };

            await _unitOfWork.ContactRepository.InsertAsync(contact);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("User {OwnerId} added contact {ContactId}", currentUserId, request.ContactUserId);
            return new AddContactResponse
            {
                ContactId = contact.Id
            };
        }

        public async Task<IEnumerable<ContactDto>> GetContactsAsync(Guid currentUserId, string query = "")
        {
            var contacts = await GetUserContactsAsync(currentUserId, query);
            return contacts.Select(ContactProjections.ToContactDto);
        }

        public async Task<HashSet<Guid>> GetContactIdsAsync(Guid currentUserId)
        {
            return (await _unitOfWork.ContactRepository.GetAll()
                .Where(c => c.OwnerUserId == currentUserId)
                .Select(c => c.ContactUserId)
                .ToListAsync())
                .ToHashSet();
        }

        public bool IsContact(Guid currentUserId, Guid id)
        {
            return _unitOfWork.ContactRepository.GetAll().Any(x => x.OwnerUserId == currentUserId && x.ContactUserId == id);
        }

        public async Task<bool> IsContactAsync(Guid currentUserId, Guid id)
        {
            return await _unitOfWork.ContactRepository.GetAll().AnyAsync(x => x.OwnerUserId == currentUserId && x.ContactUserId == id);
        }

        private async Task<IEnumerable<Contact>> GetUserContactsAsync(Guid currentUserId, string query = "")
        {
            // CA1862: Do not replace ToLower().Contains() with Contains(StringComparison) here.
            // EF Core cannot translate the Contains(string, StringComparison) overload to SQL and will throw at runtime.
            #pragma warning disable CA1862
            return await _unitOfWork.ContactRepository.GetAll().Where(x => x.OwnerUserId == currentUserId
                && x.ContactUser!.Username.ToLower().Contains(query.ToLower())).Include(x => x.ContactUser).ToListAsync();
            #pragma warning restore CA1862
        }
    }
}
