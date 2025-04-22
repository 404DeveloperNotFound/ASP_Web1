namespace WebApplication1.Interfaces
{
    public interface IProduct
    {
        string Name { get; set; }
        double Price { get; set; }
        string ImageUrl { get; set; }
        string? SerialNumber { get; set; }
    }

}
