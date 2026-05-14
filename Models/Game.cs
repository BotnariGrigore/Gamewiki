using System;
using System.Collections.Generic;

namespace GameWikiApp.Models
{
    public class Game
    {
        public int GameId { get; set; }
        public int CreatedBy { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string? FullDescription { get; set; }
        public string? CoverImage { get; set; }
        public string? BannerImage { get; set; }
        public int PopularityScore { get; set; }
        public int ArticleCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Genres { get; set; } = new();

        public override string ToString() => Title;
    }
}
