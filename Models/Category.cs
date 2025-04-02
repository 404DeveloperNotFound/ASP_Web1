namespace WebApplication1.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Items>? Items { get; set; }

    }
}
