using System.Collections.Generic;
using System.Threading.Tasks;
using GameWikiApp.Data;
using GameWikiApp.Models;

namespace GameWikiApp.Services
{
    public class ChatService
    {
        private readonly ChatRepository _repo = new();

        public Task<int> CreateConversationAsync(int createdBy, string? name, bool isGroup)
            => _repo.CreateConversationAsync(createdBy, name, isGroup);

        public Task<int> SendMessageAsync(Message message) => _repo.SendMessageAsync(message);

        public Task<IEnumerable<Message>> GetMessagesAsync(int conversationId) => _repo.GetMessagesAsync(conversationId);
    }
}
