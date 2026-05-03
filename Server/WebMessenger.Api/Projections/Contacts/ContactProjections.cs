using System.Linq.Expressions;
using WebMessenger.Api.Projections.Users;
using WebMessenger.Contracts.Models;
using WebMessenger.DAL.Entities;

namespace WebMessenger.Api.Projections.Contacts
{
    public static class ContactProjections
    {
        /// <summary>
        /// In-memory projection from a fully loaded <see cref="Contact"/> (with <c>ContactUser</c> included)
        /// to <see cref="ContactDto"/>.
        /// <para>
        /// Note: this cannot be an EF-translatable <see cref="Expression{TDelegate}"/> because it calls
        /// <see cref="GetDisplayNickname"/>, which contains conditional string logic not supported by
        /// EF Core's SQL translator. Use after <c>.Include(x => x.ContactUser).ToListAsync()</c>.
        /// </para>
        /// </summary>
        public static readonly Func<Contact, ContactDto> ToContactDto = contact => new ContactDto
        {
            Id = contact.Id,
            UserId = contact.ContactUserId,
            Nickname = GetDisplayNickname(contact),
            AvatarUrl = contact.ContactUser?.AvatarUrl,
            IsOnline = contact.ContactUser?.IsOnline ?? false,
            AddedAt = contact.AddedAt,
            ContactUser = contact.ContactUser != null
                ? UserProjections.ToUserDtoFunc(contact.ContactUser)
                : null,
            ContactUserId = contact.ContactUserId,
            OwnerUserId = contact.OwnerUserId
        };

        private static string GetDisplayNickname(Contact contact) =>
            !string.IsNullOrWhiteSpace(contact.Nickname)
                ? contact.Nickname
                : $"{contact.ContactUser?.FirstName} {contact.ContactUser?.LastName}";
    }
}
