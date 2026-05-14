namespace GameWikiApp.Models
{
    public class ArticleLink
    {
        public int LinkId { get; set; }
        public int FromArticleId { get; set; }
        public int ToArticleId { get; set; }
        public string? LinkText { get; set; }
        public string? TargetTitle { get; set; }
        public string? TargetSlug { get; set; }
        public string? TargetGameTitle { get; set; }
    }
}
