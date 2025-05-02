using WebApplication1.Models;

namespace WebApplication1.DataTransferObjects
{
    public record PaymentDto(
        List<CartItemDto> CartItems,
        decimal TotalAmount,
        AddressDto SelectedAddress
    );
    public record ConfirmPaymentDto(
        List<CartItemDto> CartItems,
        decimal TotalAmount,
        Address Address
    );
    public record OrderItemDto(
        int ItemId,
        string ItemName,
        int Quantity,
        decimal Price
    );
    public record OrderDto(
        int Id,
        DateTime OrderDate,
        string Status,
        AddressDto Address,
        List<OrderItemDto> Items,
        decimal TotalAmount
    );
    public record OrderSummaryDto(
        int Id,
        DateTime OrderDate,
        string Status,
        decimal TotalAmount
    );
}
