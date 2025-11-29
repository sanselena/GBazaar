using Microsoft.AspNetCore.Mvc;

namespace GBazaar.Controllers
{
    public class BuyerController : Controller
    {
        // GET: /Buyer/Profile
        public IActionResult Profile()
        {
            ViewBag.UserType = "Buyer";
            return View();
        }
    }
}
