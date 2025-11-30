using Gbazaar.Data;
using GBazaar.Models;
using GBazaar.Models.Enums;
using GBazaar.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace GBazaar.Controllers
{
    public class AuthController : Controller
    {
        private readonly ProcurementContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthController(ProcurementContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        public IActionResult Login() => View();

        public IActionResult Signup() => View();

        [HttpPost]
        //terms and cond ekle
        public IActionResult SignupBuyer(SignupBuyerVM model)
        {
            if (!ModelState.IsValid)
                return View("Signup", model);

            var email = model.Email.Trim().ToLowerInvariant(); //olmali mi idk belki silerim

            if (_context.Suppliers.Any(s => s.ContactInfo.ToLower() == email))
            {
                ModelState.AddModelError("", "This email is already registered as a supplier.");
                return View("Signup", model);
            }

            if (_context.Users.Any(u => u.Email.ToLower() == email))
            {
                ModelState.AddModelError("", "Email is already registered.");
                return View("Signup", model);
            }

            var deptName = $"{model.CompanyName?.Trim()} {model.Department?.Trim()}".Trim().ToLower();
            var dept = _context.Departments.FirstOrDefault(d => d.DepartmentName.ToLower() == deptName);

            if (dept == null)
            {
                dept = new Department
                {
                    DepartmentName = $"{model.CompanyName?.Trim()} {model.Department?.Trim()}"
                };
                _context.Departments.Add(dept);
            }

            var user = new User
            {
                FullName = model.FullName?.Trim(),
                Email = email,
                DepartmentID = dept.DepartmentID
                //role eklencek
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            try
            {
                _context.Users.Add(user);
                _context.SaveChanges();
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred while creating the account.");
                return View("Signup", model);
            }

            return RedirectToAction("Login");
        }

        [HttpPost]
        public IActionResult SignupSupplier(SignupSupplierVM model)
        {
            if (!ModelState.IsValid)
                return View("Signup", model);

            var email = model.ContactInfo.Trim().ToLowerInvariant();

            if (_context.Suppliers.Any(s => s.TaxID == model.TaxId))
            {
                ModelState.AddModelError("", "Tax ID is already registered.");
                return View("Signup", model);
            }

            if (_context.Users.Any(u => u.Email.ToLower() == email))
            {
                ModelState.AddModelError("", "This email is already registered as a buyer.");
                return View("Signup", model);
            }

            if (!model.AcceptTerms)
            {
                ModelState.AddModelError("", "You must accept the terms and conditions.");
                return View("Signup", model);
            }

            var supplier = new Supplier
            {
                SupplierName = model.BusinessName?.Trim(),
                TaxID = model.TaxId?.Trim(),
                ContactInfo = email
                // simdilik Password yok 
            };

            try
            {
                _context.Suppliers.Add(supplier);
                _context.SaveChanges();
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred while creating supplier account.");
                return View("Signup", model);
            }

            return RedirectToAction("Login");
        }

        [HttpPost]
        public IActionResult Login(LoginVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim().ToLowerInvariant();

            var user = _context.Users.SingleOrDefault(u => u.Email.ToLower() == email);

            if (user != null)
            {
                var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
                if (verify == PasswordVerificationResult.Success ||
                    verify == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    //cookie?
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            var supplier = _context.Suppliers.SingleOrDefault(s => s.ContactInfo.ToLower() == email);

            if (supplier != null)
            {
                // simdilik passwordsuz giris ok
                return RedirectToAction("Dashboard", "Supplier", new { id = supplier.SupplierID });
            }

            ModelState.AddModelError("", "Account not found.");
            return View(model);
        }
    }
}
