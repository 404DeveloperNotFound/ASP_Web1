using Microsoft.EntityFrameworkCore;
using WebApplication1.DataTransferObjects;
using WebApplication1.Models;

namespace WebApplication1.Interfaces
{
    public interface IItemService
    {
        Task<PaginatedList<ItemDto>> GetPagedItemsAsync(ItemQueryParameters parms);
        Task<ItemDto> GetByIdAsync(int id);
        Task<List<Category>> GetCategoriesAsync();
        Task CreateAsync(CreateItemDto dto);
        Task UpdateAsync(UpdateItemDto dto);
        Task DeleteAsync(int id);
    }
}
