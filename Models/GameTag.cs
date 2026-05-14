namespace GameWikiApp.Models
{
    public class GameTag
    {
        public int TagId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public int GameCount { get; set; }
    }
}