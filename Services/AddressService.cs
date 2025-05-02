using WebApplication1.Data;
using WebApplication1.DataTransferObjects;
using WebApplication1.Models;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Interfaces;

namespace WebApplication1.Services;

public class AddressService : IAddressService
{
    private readonly Web1Context _context;

    public AddressService(Web1Context context)
    {
        _context = context;
    }

    public async Task<List<AddressDto>> GetUserAddressesAsync(int clientId)
    {
        return await _context.Addresses
            .Where(a => a.ClientId == clientId)
            .Select(a => new AddressDto(a.Id, a.StreetAddress, a.City, a.State, a.Country, a.PostalCode, a.IsDefault))
            .ToListAsync();
    }

    public List<AddressDto> GetUserAddresses(int clientId)
    {
        return _context.Addresses
            .Where(a => a.ClientId == clientId)
            .Select(a => new AddressDto(a.Id, a.StreetAddress, a.City, a.State, a.Country, a.PostalCode, a.IsDefault))
            .ToList();
    }

    public async Task CreateAddressAsync(AddressDto addressDto, int clientId)
    {
        var address = new Address
        {
            StreetAddress = addressDto.StreetAddress,
            City = addressDto.City,
            State = addressDto.State,
            Country = addressDto.Country,
            PostalCode = addressDto.PostalCode,
            ClientId = clientId,
            IsDefault = false
        };

        _context.Add(address);
        await _context.SaveChangesAsync();
    }

    public async Task SetDefaultAddressAsync(int clientId, int addressId)
    {
        var addresses = await _context.Addresses.Where(a => a.ClientId == clientId).ToListAsync();
        foreach (var addr in addresses)
        {
            addr.IsDefault = addr.Id == addressId;
        }
        await _context.SaveChangesAsync();
    }
}
