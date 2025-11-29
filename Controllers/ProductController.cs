using Microsoft.AspNetCore.Mvc;

namespace GBazaar.Controllers
{
    public class ProductController : Controller
    {
        // GET: /Product/Details
        public IActionResult Details()
        {
            // This tells ASP.NET to look for: Views/Product/Details.cshtml
            return View();
        }
    }
}