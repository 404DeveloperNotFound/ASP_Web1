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

        public async Task<IActionResult> Index()
        {
            var items = await _context.Items.Include(s => s.SerailNumber)
                                            .Include(c => c.Category)
                                            .Include(c => c.Clients)
                                            .ToListAsync();
            return View(items);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id","Name","Price", "CategoryId")] Items item)
        {
            if (!ModelState.IsValid) // Check for validation errors
            {
                return View(item); 
            }
            _context.Add(item);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
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
        public async Task<IActionResult> Edit(int id, [Bind("Id","Name","Price","CategoryId")] Items item)
        {
            if (ModelState.IsValid)
            {
                _context.Update(item);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(item);
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.Items.Include(i => i.Category).Include(i => i.SerailNumber).Include(i => i.Clients).FirstOrDefaultAsync(i => i.Id == id);

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
            var item = await _context.Items.Include(s => s.SerailNumber).FirstOrDefaultAsync(i => i.Id == id);
            if(item != null)
            {
                if (item.SerailNumber != null)
                {
                    _context.SerialNumbers.Remove(item.SerailNumber);
                }
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

    }
}
