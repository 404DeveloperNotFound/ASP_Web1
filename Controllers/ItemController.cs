using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
namespace WebApplication1.Controllers
{
    [Authorize]
    public class ItemController : Controller
    {
        private readonly Web1Context _context;

        public ItemController(Web1Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search, int? categoryId, string sortOrder)
        {
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentCategory"] = categoryId;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name");

            var items = _context.Items.Include(i => i.Category).Include(i => i.Clients).AsQueryable();

            // Filtering by search
            if (!string.IsNullOrEmpty(search))
            {
                items = items.Where(i => i.Name.Contains(search) || i.SerialNumber.Contains(search));
            }

            // Filtering by category
            if (categoryId.HasValue)
            {
                items = items.Where(i => i.CategoryId == categoryId.Value);
            }

            // Sorting
            items = sortOrder switch
            {
                "price_asc" => items.OrderBy(i => i.Price),
                "price_desc" => items.OrderByDescending(i => i.Price),
                "name_asc" => items.OrderBy(i => i.Name),
                "name_desc" => items.OrderByDescending(i => i.Name),
                _ => items.OrderBy(i => i.Name)
            };

            return View(await items.ToListAsync());
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id","Name","Price", "ImageUrl", "CategoryId","SerialNumber")] Items item)
        {
            if (!ModelState.IsValid)
            {
                return View(item); 
            }
            _context.Add(item);
                await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Admin");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name");
            var item = await _context.Items.FirstOrDefaultAsync(x => x.Id == id);

            return View(item);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id","Name","Price", "ImageUrl", "CategoryId","SerialNumber")] Items item)
        {
            if (ModelState.IsValid)
            {
                var existingItem = await _context.Items.FindAsync(id);
                if (existingItem == null)
                {
                    return NotFound();
                }

                existingItem.Name = item.Name;
                existingItem.Price = item.Price;
                existingItem.CategoryId = item.CategoryId;
                existingItem.ImageUrl = item.ImageUrl;

                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Admin");
            }
            return View(item);
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.Items.Include(i => i.Category).Include(i => i.Clients).FirstOrDefaultAsync(i => i.Id == id);

            if(item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Items.FindAsync(id);
            return View(item);
        }
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirm(int id)
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);
            if(item != null)
            {
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Admin");
        }
    }
}
