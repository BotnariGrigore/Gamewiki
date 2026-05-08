namespace GameWikiApp.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public int GameId { get; set; }

        public string CategoryName { get; set; }
        public string Description { get; set; }
    }
}