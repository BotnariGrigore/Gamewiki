using System;
using System.Collections.Generic;

namespace GameWikiApp.Models
{
    public class Game
    {
        public int GameId { get; set; }
        public int CreatedBy { get; set; }

        public string Title { get; set; }
        public string Slug { get; set; }

        public string ShortDescription { get; set; }
        public string FullDescription { get; set; }

        public string CoverImage { get; set; }
        public string BannerImage { get; set; }

        public DateTime CreatedAt { get; set; }

        // navigation helpers (optional)
        public List<Tag> Tags { get; set; } = new List<Tag>();
    }
}