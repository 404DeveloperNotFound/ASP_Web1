using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Threading.Tasks;
using System.Linq;

namespace WebApplication1.Controllers
{
    public class AdminController : Controller
    {
        private readonly Web1Context _context;

        public AdminController(Web1Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return RedirectToAction("ManageItems");
        }
        public async Task<IActionResult> ManageItems()
        {
            var items = await _context.Items
                .Include(i => i.SerailNumber)
                .Include(i => i.Category)
                .Include(i=>i.Clients)
                .ToListAsync();

            ViewBag.ViewType = "Items";
            return View("Dashboard", items);
        }

        public async Task<IActionResult> ManageUsers()
        {
            var users = await _context.Clients.Where(u => u.Role != "Admin").ToListAsync();
            ViewBag.ViewType = "Users";
            return View("Dashboard", users);
        }

        public async Task<IActionResult> PromoteToAdmin(int id)
        {
            var user = await _context.Clients.FindAsync(id);
            if (user == null) return NotFound();

            user.Role = "Admin";
            await _context.SaveChangesAsync();
            return RedirectToAction("ManageUsers");
        }

        public async Task<IActionResult> Blacklist(int id)
        {
            var user = await _context.Clients.FindAsync(id);
            if (user == null) return NotFound();

            user.IsBlocked = true;
            await _context.SaveChangesAsync();
            return RedirectToAction("ManageUsers");
        }
    }
}
