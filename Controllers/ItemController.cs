using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.DataTransferObjects;
using WebApplication1.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[Authorize]
public class ItemController : Controller
{
    private readonly IItemService _itemService;
    private const int PageSize = 10;

    public ItemController(IItemService itemService)
    {
        _itemService = itemService;
    }

    public async Task<IActionResult> Index(
        string? search,
        int? categoryId,
        string? sortOrder,
        int? pageNumber)
    {
        try
        {
            var parms = new ItemQueryParameters(
                search,
                categoryId,
                sortOrder,
                pageNumber ?? 1,
                PageSize
            );
             
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentCategory"] = categoryId;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["Categories"] = new SelectList(await _itemService.GetCategoriesAsync(), "Id", "Name");


            var paged = await _itemService.GetPagedItemsAsync(parms);
            return View(paged);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Unable to load items.");
            return View(new PaginatedList<ItemDto>(new List<ItemDto>(), 0, 1, PageSize));
        }
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        ViewData["Categories"] = new SelectList(await _itemService.GetCategoriesAsync(), "Id", "Name");
        return View();
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateItemDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        try
        {
            await _itemService.CreateAsync(dto);
            return RedirectToAction("Index", "Admin");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var item = await _itemService.GetByIdAsync(id);
            ViewData["Categories"] = new SelectList(await _itemService.GetCategoriesAsync(), "Id", "Name");
            var updateDto = new UpdateItemDto(
                item.Id, item.Name, item.Price, item.ImageUrl,
                item.CategoryId, item.SerialNumber, item.Quantity
            );
            return View(updateDto);
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(UpdateItemDto dto)
    {
        if (!ModelState.IsValid)
        {
            // Logging validation errors
            foreach (var entry in ModelState)
            {
                var fieldName = entry.Key;
                foreach (var error in entry.Value.Errors)
                {
                    var message = string.IsNullOrEmpty(error.ErrorMessage)
                        ? error.Exception?.Message
                        : error.ErrorMessage;

                    Console.WriteLine($"Validation error on '{fieldName}': {message}");
                }
            }
            return View(dto);
        }

        try
        {
            await _itemService.UpdateAsync(dto);
            return RedirectToAction("Index", "Admin");
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError("", "Concurrency conflict — please try again.");
            return View(dto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "Edit failed.");
            return View(dto);
        }
    }

    [Authorize]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var item = await _itemService.GetByIdAsync(id);
            return View(item);
        }
        catch
        {
            return NotFound();
        }
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var item = await _itemService.GetByIdAsync(id);
            return View(item);
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpPost, ActionName("Delete"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirm(int id)
    {
        try
        {
            await _itemService.DeleteAsync(id);
            return RedirectToAction("Index", "Admin");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch
        {
            return BadRequest();
        }
    }
}
