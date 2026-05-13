using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameWikiApp.Data;
using GameWikiApp.Models;

namespace GameWikiApp.Services
{
    public class ConversationService
    {
        private readonly ConversationRepository _repo = new();

        public Task<IEnumerable<Conversation>> GetByUserIdAsync(int userId) => _repo.GetByUserIdAsync(userId);
        public Task<Conversation?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<int> CreateAsync(Conversation conversation) => _repo.CreateAsync(conversation);
        public Task<IEnumerable<ConversationParticipant>> GetParticipantsAsync(int conversationId) => _repo.GetParticipantsAsync(conversationId);
        public Task AddParticipantAsync(int conversationId, int userId) => _repo.AddParticipantAsync(conversationId, userId);
        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }

    public class CommentService
    {
        private readonly CommentRepository _repo = new();

        public Task<IEnumerable<CommentWithUser>> GetByArticleIdAsync(int articleId) => _repo.GetByArticleIdAsync(articleId);
        public Task<int> CreateAsync(Comment comment) => _repo.CreateAsync(comment);
        public Task<bool> DeleteAsync(int commentId) => _repo.DeleteAsync(commentId);
    }

    public class LikeService
    {
        private readonly LikeRepository _repo = new();

        public Task<int> GetCountAsync(int articleId) => _repo.GetCountAsync(articleId);
        public Task<bool> HasLikedAsync(int articleId, int userId) => _repo.HasLikedAsync(articleId, userId);
        public Task<bool> ToggleLikeAsync(int articleId, int userId) => _repo.ToggleLikeAsync(articleId, userId);
    }

    public class SavedArticleService
    {
        private readonly SavedArticleRepository _repo = new();

        public Task<bool> IsSavedAsync(int articleId, int userId) => _repo.IsSavedAsync(articleId, userId);
        public Task<bool> ToggleSaveAsync(int articleId, int userId) => _repo.ToggleSaveAsync(articleId, userId);
        public Task<IEnumerable<WikiArticle>> GetSavedArticlesAsync(int userId) => _repo.GetSavedArticlesAsync(userId);
    }
}