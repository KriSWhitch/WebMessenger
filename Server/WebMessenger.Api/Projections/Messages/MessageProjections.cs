using System.Linq.Expressions;
using WebMessenger.Contracts.Models;
using WebMessenger.DAL.Entities;

namespace WebMessenger.Api.Projections.Messages
{
    public static class MessageProjections
    {
        /// <summary>
        /// EF-translatable projection from <see cref="Message"/> to <see cref="ChatMessageDto"/>.
        /// </summary>
        public static readonly Expression<Func<Message, ChatMessageDto>> ToChatMessageDto =
            m => new ChatMessageDto
            {
                Id = m.Id,
                ChatId = m.ChatId,
                SenderId = m.SenderId,
                Content = m.Content,
                SentAt = m.SentAt,
                EditedAt = m.EditedAt
            };

        /// <summary>
        /// Compiled in-memory version of <see cref="ToChatMessageDto"/> for mapping
        /// freshly created <see cref="Message"/> entities (not yet in the DB query pipeline).
        /// </summary>
        public static readonly Func<Message, ChatMessageDto> ToChatMessageDtoFunc = ToChatMessageDto.Compile();
    }
}
