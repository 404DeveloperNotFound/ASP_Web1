using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;
using WebApplication1.DataTransferObjects;
using System.Security.Claims;
using WebApplication1.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class AddressController : Controller
    {
        private readonly IAddressService _addressService;

        public AddressController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        public async Task<IActionResult> Index()
        { 
            try
            {
                var clientId = GetCurrentUserId();
                var addresses = await _addressService.GetUserAddressesAsync(clientId);
                return View(addresses);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error loading addresses."); 
                return View(new List<AddressDto>());
            }
        }

        public IActionResult Create(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index", "Address");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(AddressDto addressDto, string returnUrl)
        {
            if (!ModelState.IsValid)
                return View(addressDto);

            try
            {
                var clientId = GetCurrentUserId();
                await _addressService.CreateAddressAsync(addressDto, clientId);
                return RedirectToLocal(returnUrl);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to add address.");
                return View(addressDto);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetDefault(int id)
        {
            try
            {
                var clientId = GetCurrentUserId();
                await _addressService.SetDefaultAddressAsync(clientId, id);
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to set default address.");
                return RedirectToAction("Index");
            }
        }

        public IActionResult Select(string returnUrl = null)
        {
            try
            {
                if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
                    returnUrl = Url.Action("Payment", "Order");

                ViewBag.ReturnUrl = returnUrl;

                var clientId = GetCurrentUserId();
                var addresses = _addressService.GetUserAddresses(clientId);
                return View(addresses);
            }
            catch
            {
                ModelState.AddModelError("", "Failed to load addresses.");
                return View(new List<AddressDto>());
            }
        }

        [HttpPost]
        public IActionResult Select(int addressId, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
                returnUrl = Url.Action("Payment", "Order");

            HttpContext.Session.SetInt32("SelectedAddressId", addressId);
            return Redirect(returnUrl);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index");
        }
    }
}
