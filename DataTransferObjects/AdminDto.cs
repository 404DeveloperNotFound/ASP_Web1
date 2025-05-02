namespace WebApplication1.DataTransferObjects
{
    public record AdminItemDto(int Id,
    string Name,
    decimal Price,
    string SerialNumber,
    string? CategoryName,
    int Quantity);
    public record AdminClientDto(int Id, string Username, string Email, string Role, bool IsBlocked);
}