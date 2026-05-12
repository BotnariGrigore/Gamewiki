using System;

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
        public DateTime CreatedAt { get; set; }
    }
}
