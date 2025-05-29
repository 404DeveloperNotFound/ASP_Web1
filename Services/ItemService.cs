using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebApplication1.Data;
using WebApplication1.DataTransferObjects;
using WebApplication1.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class ItemService : IItemService
    {
        private readonly Web1Context _context;
        private const int DefaultPageSize = 10;
         
        public ItemService(Web1Context context)
        {
            _context = context;
        }

        public async Task<PaginatedList<ItemDto>> GetPagedItemsAsync(ItemQueryParameters parms)
        {
            IQueryable<Items> items = _context.Items
                .Include(i => i.Category);

            // Filtering
            if (!string.IsNullOrEmpty(parms.Search))
            {
                items = items.Where(i =>
                    i.Name.Contains(parms.Search) ||
                    i.SerialNumber.Contains(parms.Search));
            }

            if (parms.CategoryId.HasValue)
            {
                items = items.Where(i => i.CategoryId == parms.CategoryId.Value);
            }

            // Sorting
            items = parms.SortOrder switch
            {
                "price_asc" => items.OrderBy(i => i.Price),
                "price_desc" => items.OrderByDescending(i => i.Price),
                "name_asc" => items.OrderBy(i => i.Name),
                "name_desc" => items.OrderByDescending(i => i.Name),
                _ => items.OrderBy(i => i.Name)
            };

            
            return await PaginatedList<ItemDto>
                .CreateAsync(
                items.Select(i =>
                    new ItemDto
                    (
                        i.Id,
                        i.Name,
                        (decimal)i.Price,
                        i.ImageUrl,
                        i.CategoryId,
                        i.Category != null ? i.Category.Name : "",
                        i.SerialNumber,
                        i.Quantity,
                        i.RowVersion
                    )
                ).AsNoTracking(),
                parms.PageNumber > 0 ? parms.PageNumber : 1,
                parms.PageSize);
        }

        public async Task<ItemDto> GetByIdAsync(int id)
        {
            var i = await _context.Items
                .Include(x => x.Category)
                .Include(x => x.Clients)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Item not found");

            return new ItemDto(
                i.Id, i.Name, (decimal)i.Price, i.ImageUrl,
                i.CategoryId, i.Category?.Name ?? "",
                i.SerialNumber, i.Quantity, i.RowVersion
            );
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _context.Categories.AsNoTracking().ToListAsync();
        }

        public async Task CreateAsync(CreateItemDto dto)
        {
            var item = new Items
            {
                Name = dto.Name,
                Price = (double)dto.Price,
                ImageUrl = dto.ImageUrl,
                CategoryId = dto.CategoryId,
                SerialNumber = dto.SerialNumber,
                Quantity = dto.Quantity
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(UpdateItemDto dto)
        {
            var existing = await _context.Items.FindAsync(dto.Id)
                ?? throw new KeyNotFoundException("Item not found");
            // concurrency
            //_context.Entry(existing).OriginalValues["RowVersion"] = dto.RowVersion;

            existing.Name = dto.Name;
            existing.Price = (double)dto.Price;
            existing.ImageUrl = dto.ImageUrl;
            existing.CategoryId = dto.CategoryId;
            existing.SerialNumber = dto.SerialNumber;
            existing.Quantity = dto.Quantity;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _context.Items.FindAsync(id)
                ?? throw new KeyNotFoundException("Item not found");
            _context.Items.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }
}
