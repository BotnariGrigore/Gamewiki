using System;

namespace GameWikiApp.Models
{
    public class WikiArticle
    {
        public int ArticleId { get; set; }
        public int GameId { get; set; }
        public int AuthorId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? CoverImage { get; set; }
        public int ViewsCount { get; set; }
        public bool IsPublished { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
