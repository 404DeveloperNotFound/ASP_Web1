using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Security.Claims;

namespace WebApplication1.Controllers
{
    public class AddressController : Controller
    {
        private readonly Web1Context _context;

        public AddressController(Web1Context context)
        {
            _context = context;
        }

        private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        public async Task<IActionResult> Index()
        {
            var clientId = GetCurrentUserId();
            var addresses = await _context.Addresses.Where(a => a.ClientId == clientId).ToListAsync();
            return View(addresses);
        }

        public IActionResult Create(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index", "Address");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("StreetAddress","City","State","Country","PostalCode")] Address address, string returnUrl)
        {
            Console.WriteLine(GetCurrentUserId());
            if (!ModelState.IsValid)
            {
                return View(address);
            }

            address.ClientId = GetCurrentUserId();
            _context.Add(address);
            await _context.SaveChangesAsync();
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetDefault(int id)
        {
            var clientId = GetCurrentUserId();

            var addresses = await _context.Addresses.Where(a => a.ClientId == clientId).ToListAsync();

            foreach (var addr in addresses)
                addr.IsDefault = (addr.Id == id);

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public IActionResult Select(string returnUrl = null)
        {
            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = Url.Action("Payment", "Order");
            }

            ViewBag.ReturnUrl = returnUrl;
            var clientId = GetCurrentUserId();
            var addresses = _context.Addresses.Where(a => a.ClientId == clientId).ToList();
            return View(addresses);
        }

        [HttpPost]
        public IActionResult Select(int addressId, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = Url.Action("Payment", "Order");
            }

            ViewBag.ReturnUrl = returnUrl;
            HttpContext.Session.SetInt32("SelectedAddressId", addressId);
            return Redirect(returnUrl);
        }
    }
}
