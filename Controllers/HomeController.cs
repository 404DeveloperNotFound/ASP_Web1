using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Interfaces;
using WebApplication1.ViewModel;   

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRoleRedirectService _redirectService;

        public HomeController(ILogger<HomeController> logger, IRoleRedirectService redirectService)
        {
            _logger = logger;
            _redirectService = redirectService;
        }

        public IActionResult Index()
        {
            return _redirectService.GetRedirectForUser(User, this);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [AllowAnonymous]
        public IActionResult Blacklisted() => View();
    }
}
