using WebApplication1.DataTransferObjects;

namespace WebApplication1.Interfaces
{
    public interface IAdminService
    {
        Task<List<AdminItemDto>> GetAllItemsAsync();
        Task<List<AdminClientDto>> GetUsersAsync();
        Task<List<AdminClientDto>> GetAdminsAsync();
        Task PromoteUserToAdminAsync(int id);
        Task BlacklistUserAsync(int id);
        Task UnBlacklistUserAsync(int id);
    }
}
