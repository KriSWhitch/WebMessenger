using Microsoft.EntityFrameworkCore;
using WebMessenger.Api.Models;
using WebMessenger.Api.Services.Interfaces;
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
                OwnerUser = _unitOfWork.UserRepository.Get(currentUserId),
                ContactUserId = request.ContactUserId,
                ContactUser = _unitOfWork.UserRepository.Get(request.ContactUserId),
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

            return contacts.Select(ConvertToContactDto);
        }

        private ContactDto ConvertToContactDto(Contact contact)
        {
            return new ContactDto
            {
                Id = contact.Id,
                UserId = contact.ContactUserId,
                Nickname = GetDisplayNickname(contact),
                AvatarUrl = contact.ContactUser?.AvatarUrl,
                IsOnline = contact.ContactUser?.IsOnline ?? false,
                AddedAt = contact.AddedAt,
                ContactUser = contact.ContactUser != null ? ConvertToUserDto(contact.ContactUser) : null,
                ContactUserId = contact.ContactUserId,
                OwnerUserId = contact.OwnerUserId
            };
        }

        private static UserDto ConvertToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl,
                IsOnline = user.IsOnline,
                LastSeenAt = user.LastSeenAt,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        private static string GetDisplayNickname(Contact contact)
        {
            return !string.IsNullOrWhiteSpace(contact.Nickname)
                ? contact.Nickname
                : $"{contact.ContactUser?.FirstName} {contact.ContactUser?.LastName}";
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
