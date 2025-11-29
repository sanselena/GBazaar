using Microsoft.AspNetCore.Mvc;

namespace GBazaar.Controllers
{
    public class SupplierController : Controller
    {
        // GET: /Supplier/Dashboard
        public IActionResult Dashboard()
        {
            ViewBag.UserType = "Supplier";
            return View();
        }
    }
}
