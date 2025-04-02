namespace WebApplication1.Models
{
    public class ItemClient
    {
        public int ClientId { get; set; }
        public Client Client { get; set; }

        public int ItemId { get; set; }
        public Items Item { get; set; }
    }
}
