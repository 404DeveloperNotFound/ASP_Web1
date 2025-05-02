namespace WebApplication1.DataTransferObjects
{
    public record ItemDto(
        int Id,
        string Name,
        decimal Price,
        string ImageUrl,
        int? CategoryId,
        string CategoryName,
        string SerialNumber,
        int Quantity,
        byte[] RowVersion
    );

    public record CreateItemDto(
        string Name,
        decimal Price,
        string ImageUrl,
        int CategoryId,
        string SerialNumber,
        int Quantity
    );

    public record UpdateItemDto(
        int Id,
        string Name,
        decimal Price,
        string ImageUrl,
        int? CategoryId,
        string SerialNumber,
        int Quantity
    );

    public record ItemQueryParameters(
        string? Search,
        int? CategoryId,
        string? SortOrder,
        int PageNumber,
        int PageSize
    );
}
