namespace WebApplication1.DataTransferObjects
{
    public record AddressDto(
      int Id,
      string StreetAddress,
      string City,
      string State,
      string Country,
      string PostalCode,
      bool IsDefault
  );
}
