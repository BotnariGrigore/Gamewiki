using System;
using System.Collections.Generic;

namespace GameWikiApp.Models
{
    public class WikiArticle
    {
        public int ArticleId { get; set; }
        public int GameId { get; set; }
        public int AuthorId { get; set; }

        public string Title { get; set; }
        public string Slug { get; set; }

        public string Summary { get; set; }
        public string Content { get; set; }

        public string CoverImage { get; set; }

        public int ViewsCount { get; set; } = 0;
        public bool IsPublished { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // navigation helpers
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Tag> Tags { get; set; } = new List<Tag>();
    }
}