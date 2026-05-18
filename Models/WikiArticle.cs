using System;
using System.Linq;

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
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public bool IsPublished { get; set; } = true;
        public string? GameTitle { get; set; }
        public string? AuthorUsername { get; set; }
        public string? CategoryNames { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string PrimaryCategoryName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CategoryNames))
                {
                    return "Uncategorized";
                }

                var first = CategoryNames
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .FirstOrDefault();

                return string.IsNullOrWhiteSpace(first) ? "Uncategorized" : first;
            }
        }

        public int CategoryCount
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CategoryNames))
                {
                    return 0;
                }

                return CategoryNames
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Length;
            }
        }

        public string CategoryLabel
        {
            get
            {
                var primary = PrimaryCategoryName;
                var count = CategoryCount;
                return count > 1 ? $"{primary} +{count - 1}" : primary;
            }
        }

        public override string ToString() => Title;
    }
}
