using Microsoft.EntityFrameworkCore;
using WebMessenger.Api.Models;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.DAL;
using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;

namespace WebMessenger.Api.Services
{
    public class ContactsService(IUnitOfWork unitOfWork) : IContactsService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<AddContactResponse> AddContactAsync(Guid currentUserId, AddContactRequest request)
        {
            if (currentUserId == request.ContactUserId)
                throw new InvalidOperationException("Cannot add yourself as a contact");

            if (await IsContactAsync(currentUserId, request.ContactUserId))
                throw new InvalidOperationException("User is already in your contacts");

            var contact = new Contact
            {
                OwnerUserId = currentUserId,
                ContactUserId = request.ContactUserId,
                Nickname = request.Nickname,
                AddedAt = DateTime.UtcNow
            };

            _unitOfWork.ContactRepository.Insert(contact);
            await _unitOfWork.CommitAsync();

            return new AddContactResponse
            {
                ContactId = contact.Id
            };
        }

        public async Task<IEnumerable<ContactDto>> GetContactsAsync(Guid currentUserId, string query = "")
        {
            var contacts = await GetUserContactsAsync(currentUserId, query);

            return contacts.Select(c => new ContactDto
            {
                Id = c.Id,
                UserId = c.ContactUserId,
                Nickname = c.Nickname ?? $"{c.ContactUser?.FirstName} {c.ContactUser?.LastName}",
                AvatarUrl = c.ContactUser?.AvatarUrl,
                IsOnline = c.ContactUser?.IsOnline ?? false,
                AddedAt = c.AddedAt,
                ContactUser = new UserDto
                {
                    Id = c.ContactUser.Id,
                    Username = c.ContactUser.Username,
                    Email = c.ContactUser.Email,
                    PhoneNumber = c.ContactUser.PhoneNumber,
                    FirstName = c.ContactUser.FirstName,
                    LastName = c.ContactUser.LastName,
                    Bio = c.ContactUser.Bio,
                    AvatarUrl = c.ContactUser.AvatarUrl,
                    IsOnline = c.ContactUser.IsOnline,
                    LastSeenAt = c.ContactUser.LastSeenAt,
                    CreatedAt = c.ContactUser.CreatedAt,
                    LastLoginAt = c.ContactUser.LastLoginAt
                },
                ContactUserId = c.ContactUserId,
                OwnerUserId = c.OwnerUserId
            });
        }

        private async Task<IEnumerable<Contact>> GetUserContactsAsync(Guid currentUserId, string query = "")
        {
            return await _unitOfWork.ContactRepository.GetAll().Where(x => x.OwnerUserId == currentUserId 
                && x.ContactUser.Username.ToLower().Contains(query.ToLower())).Include(x => x.ContactUser).ToListAsync();
        }

        public bool IsContact(Guid currentUserId, Guid id)
        {
            return _unitOfWork.ContactRepository.GetAll().Any(x => x.OwnerUserId == currentUserId && x.ContactUserId == id);
        }

        public async Task<bool> IsContactAsync(Guid currentUserId, Guid id)
        {
            return await _unitOfWork.ContactRepository.GetAll().AnyAsync(x => x.OwnerUserId == currentUserId && x.ContactUserId == id);
        }
    }
}
