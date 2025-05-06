using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Interfaces;

namespace WebApplication1.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{ 
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    public IActionResult Index() => RedirectToAction("ManageItems");

    public async Task<IActionResult> ManageItems()
    {
        try
        {
            var items = await _adminService.GetAllItemsAsync();
            ViewBag.ViewType = "Items";
            return View("Dashboard", items);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public async Task<IActionResult> ManageUsers()
    {
        try
        {
            var users = await _adminService.GetUsersAsync();
            ViewBag.ViewType = "Users";
            return View("Dashboard", users);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public async Task<IActionResult> ViewAdmins()
    {
        try
        {
            var admins = await _adminService.GetAdminsAsync();
            ViewBag.ViewType = "Admins";
            return View("Dashboard", admins);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public async Task<IActionResult> PromoteToAdmin(int id)
    {
        try
        {
            await _adminService.PromoteUserToAdminAsync(id);
            return RedirectToAction("ManageUsers");
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    public async Task<IActionResult> Blacklist(int id)
    {
        try
        {
            await _adminService.BlacklistUserAsync(id);
            return RedirectToAction("ManageUsers");
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    public async Task<IActionResult> UnBlacklist(int id)
    {
        try
        {
            await _adminService.UnBlacklistUserAsync(id);
            return RedirectToAction("ManageUsers");
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }
}
