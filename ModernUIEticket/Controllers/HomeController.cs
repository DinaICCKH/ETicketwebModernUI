using ETicketNewUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Text;

namespace ETicketNewUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TicketDbContext _context;
        private readonly IWebHostEnvironment _env;

        public HomeController(
            ILogger<HomeController> logger,
            TicketDbContext context,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _context = context;
            _env = env;
        }

        // GET: Home Page with Latest Advertisements
        public async Task<IActionResult> Index()
        {
            try
            {
                // Fetch the latest advertisements
                var adsList = await _context.AdvertisementListDtos
                    .FromSqlInterpolated($@"EXEC ICC_GET_Advertisement_List")
                    .ToListAsync();

                // Pass the advertisements to the view
                return View(adsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load advertisements");
                // Optionally, show a friendly message in the view
                ViewBag.AdError = "Unable to load latest news at the moment.";
                return View(new List<AdvertisementListDto>());
            }
        }

        // GET: Privacy Policy
        public IActionResult Privacy()
        {
            return View();
        }

        // GET: Error Page
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
