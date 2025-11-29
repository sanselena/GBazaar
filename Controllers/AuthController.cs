using Microsoft.AspNetCore.Mvc;

namespace GBazaar.Controllers
{
    public class AuthController : Controller
    {
        // GET: /Auth/Login
        public IActionResult Login()
        {
            // This looks for: Views/Auth/Login.cshtml
            return View();
        }

        // GET: /Auth/Signup
        public IActionResult Signup()
        {
            // This looks for: Views/Auth/Signup.cshtml
            return View();
        }
    }
}