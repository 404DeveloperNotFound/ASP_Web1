using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Interfaces;

namespace WebApplication1.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
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
            _logger.LogError(ex, "Error fetching items for ManageItems");
            return BadRequest($"Failed to load items: {ex.Message}");
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
            _logger.LogError(ex, "Error fetching users for ManageUsers");
            return BadRequest($"Failed to load users: {ex.Message}");
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
            _logger.LogError(ex, "Error fetching admins for ViewAdmins");
            return BadRequest($"Failed to load admins: {ex.Message}");
        }
    }

    public async Task<IActionResult> PromoteToAdmin(int id)
    {
        try
        {
            await _adminService.PromoteUserToAdminAsync(id);
            return RedirectToAction("ManageUsers");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found for PromoteToAdmin, ID: {Id}", id);
            return NotFound($"User not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting user to admin, ID: {Id}", id);
            return BadRequest($"Failed to promote user: {ex.Message}");
        }
    }

    public async Task<IActionResult> Blacklist(int id)
    {
        try
        {
            await _adminService.BlacklistUserAsync(id);
            return RedirectToAction("ManageUsers");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found for Blacklist, ID: {Id}", id);
            return NotFound($"User not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blacklisting user, ID: {Id}", id);
            return BadRequest($"Failed to blacklist user: {ex.Message}");
        }
    }

    public async Task<IActionResult> UnBlacklist(int id)
    {
        try
        {
            await _adminService.UnBlacklistUserAsync(id);
            return RedirectToAction("ManageUsers");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found for UnBlacklist, ID: {Id}", id);
            return NotFound($"User not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblacklisting user, ID: {Id}", id);
            return BadRequest($"Failed to unblacklist user: {ex.Message}");
        }
    }
}