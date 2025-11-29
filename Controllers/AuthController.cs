using Gbazaar.Data;
using GBazaar.Models;
using System.Linq;
using GBazaar.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GBazaar.Controllers
{
    public class AuthController : Controller
    {
        private readonly ProcurementContext _context;

        public AuthController(ProcurementContext context)
        {
            _context = context;
        }
        public IActionResult Login()
        {
            return View();
        }
        public IActionResult Signup()
        {
            return View();
        }


        [HttpPost]
        public IActionResult SignupBuyer(string FullName,string CompanyName, string Department, string Email, string Password)
        {
           if(string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ViewBag.Error = "Email and Password are required.";
                return View("Signup");
            }

           if(_context.Users.Any(u => u.Email == Email))
            {
                ViewBag.Error = "Email is already registered.";
                return View("Signup");
            }

           var dept = _context.Departments.FirstOrDefault(d => d.DepartmentName == CompanyName + Department);
              if(dept == null)
            {
                dept = new Department
                {
                    DepartmentName = CompanyName +" "+ Department
                };
                _context.Departments.Add(dept);
                _context.SaveChanges();
            }

                var user = new User
            {
                FullName = FullName,
                DepartmentID = dept.DepartmentID,
                Email = Email,
                PasswordHash = Password, // In real application, hash the password
               // RoleID = (int)UserRole.Buyer rol eklencek
               // JobTitle = JobTitle eklencek
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        [HttpPost]
        public IActionResult SignupSupplier(string BusinessName, string TaxId, string ContactInfo, string Password, bool AcceptTerms)
        {
            if (string.IsNullOrWhiteSpace(ContactInfo) ||
                string.IsNullOrWhiteSpace(Password) ||
                string.IsNullOrWhiteSpace(BusinessName) ||
                string.IsNullOrWhiteSpace(TaxId))
            {
                ViewBag.Error = "All fields are required.";
                return View("Signup");
            }

            if (!AcceptTerms)
            {
                ViewBag.Error = "You must accept the terms and conditions.";
                return View("Signup");
            }

            if(_context.Suppliers.Any(s=>s.TaxID == TaxId))
            {
                ViewBag.Error = "Tax ID is already registered.";
                return View("Signup");
            }

            var supplier = new Supplier
            {
                SupplierName = BusinessName,
                TaxID = TaxId,
                ContactInfo = ContactInfo,
                //PasswordHash = Password // In real application, hash the password
            };

            _context.Suppliers.Add(supplier);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        [HttpPost]
        public IActionResult Login(string Email, string Password)
        {
            if(string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ViewBag.Error = "Email and Password are required.";
                return View();
            }
            
           var userExists = _context.Users.Any(u => u.Email == Email && u.PasswordHash == Password);
           var supplierExist = _context.Suppliers.Any(s => s.ContactInfo == Email /*&& s.PasswordHash == Password*/);

            if (userExists && supplierExist) {
                return RedirectToAction("Index", "Home");
            }
            
            ViewBag.Error = "Account not found";
            return View();

        }

    }
}