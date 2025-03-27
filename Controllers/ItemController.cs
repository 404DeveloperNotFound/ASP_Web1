using Microsoft.AspNetCore.Mvc;

using WebApplication1.Models;
namespace WebApplication1.Controllers
{
    public class ItemController : Controller
    {
        public IActionResult Overview()
        {
            Items item = new Items() { Name="first"};
            return View(item);
        }

        public IActionResult Edit(int ItemId)
        {
            return Content($"ItemId: {ItemId}");
        }

        public IActionResult RazorData()
        {
            return View();
        }
    }
}
