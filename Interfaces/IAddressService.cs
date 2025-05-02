using WebApplication1.DataTransferObjects;

namespace WebApplication1.Interfaces
{
    public interface IAddressService
    {
        Task<List<AddressDto>> GetUserAddressesAsync(int clientId);
        Task CreateAddressAsync(AddressDto addressDto, int clientId);
        Task SetDefaultAddressAsync(int clientId, int addressId);
        List<AddressDto> GetUserAddresses(int clientId);
    }
}
