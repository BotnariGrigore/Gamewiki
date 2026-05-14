namespace GameWikiApp.Models
{
    public class ArticleImage
    {
        public int ImageId { get; set; }
        public int ArticleId { get; set; }
        public int UploadedBy { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? AltText { get; set; }
    }
}
