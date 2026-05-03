using System.Linq.Expressions;
using WebMessenger.Contracts.Models;
using WebMessenger.DAL.Entities;

namespace WebMessenger.Api.Projections.Users
{
    public static class UserProjections
    {
        /// <summary>
        /// EF-translatable projection from <see cref="User"/> to <see cref="UserProfileDto"/>.
        /// </summary>
        public static readonly Expression<Func<User, UserProfileDto>> ToProfileDto =
            u => new UserProfileDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Bio = u.Bio,
                AvatarUrl = u.AvatarUrl,
                IsOnline = u.IsOnline
            };

        /// <summary>
        /// EF-translatable projection from <see cref="User"/> to <see cref="UserDto"/>.
        /// </summary>
        public static readonly Expression<Func<User, UserDto>> ToUserDto =
            u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Bio = u.Bio,
                AvatarUrl = u.AvatarUrl,
                IsOnline = u.IsOnline,
                LastSeenAt = u.LastSeenAt,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            };

        /// <summary>
        /// Compiled in-memory version of <see cref="ToUserDto"/> for use after entity loading.
        /// </summary>
        public static readonly Func<User, UserDto> ToUserDtoFunc = ToUserDto.Compile();

        /// <summary>
        /// Returns an EF-translatable projection from <see cref="User"/> to <see cref="UserSearchResultDto"/>.
        /// <paramref name="contactIds"/> is a local set used for the <c>IN (...)</c> filter — EF translates
        /// <see cref="HashSet{T}.Contains"/> to SQL <c>IN</c>.
        /// </summary>
        public static Expression<Func<User, UserSearchResultDto>> ToSearchResult(HashSet<Guid> contactIds) =>
            u => new UserSearchResultDto
            {
                Id = u.Id,
                Username = u.Username,
                FirstName = u.FirstName,
                LastName = u.LastName,
                AvatarUrl = u.AvatarUrl,
                IsOnline = u.IsOnline,
                IsContact = contactIds.Contains(u.Id)
            };
    }
}
