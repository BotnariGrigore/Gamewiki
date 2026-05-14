namespace GameWikiApp.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public int GameId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? GameTitle { get; set; }
        public int ArticleCount { get; set; }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(GameTitle)
                ? CategoryName
                : $"{GameTitle} / {CategoryName}";
        }
    }
}
