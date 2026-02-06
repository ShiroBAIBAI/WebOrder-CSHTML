namespace Demo.Models
{
    public class MenuItemCsvModel
    {
        public int MenuCategoryId { get; set; }   // must be INT 
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int SortOrder { get; set; }
        public string? Description { get; set; }
        public string? Recipe { get; set; }
        public bool IsAvailable { get; set; }
        public string? PhotoURL { get; set; }
    }

}
