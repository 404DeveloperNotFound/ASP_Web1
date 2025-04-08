namespace WebApplication1.Models
{
    public abstract class User 
    {
        public int Id { get; set; }
        public string Name { get; set; }
    
    }
    public class Client : User
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Items>? Items { get; set; }

        //public List<ItemClient>? ItemClients { get; set; }
    }
}
