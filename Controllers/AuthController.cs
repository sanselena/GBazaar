using Gbazaar.Data;
using GBazaar.Models;
using GBazaar.Models.Enums;
using GBazaar.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;

namespace GBazaar.Controllers
{
    public class AuthController : Controller
    {
        private readonly ProcurementContext _context;
        private readonly PasswordHasher<User> _userPasswordHasher;
        private readonly PasswordHasher<Supplier> _supplierPasswordHasher;

        public AuthController(ProcurementContext context)
        {
            _context = context;
            _userPasswordHasher = new PasswordHasher<User>();
            _supplierPasswordHasher = new PasswordHasher<Supplier>();
        }

        public IActionResult Login() => View();

        public IActionResult Signup()
        {
            // dep dropdownı
            ViewBag.Departments = _context.Departments
                .Select(d => new SelectListItem
                {
                    Value = d.DepartmentID.ToString(),
                    Text = d.DepartmentName
                })
                .ToList();

            return View();
        }

        [HttpPost]
        public IActionResult SignupBuyer(SignupBuyerVM model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = _context.Departments
                    .Select(d => new SelectListItem
                    {
                        Value = d.DepartmentID.ToString(),
                        Text = d.DepartmentName
                    })
                    .ToList();
                return View("Signup", model);
            }

            var email = model.Email.Trim().ToLowerInvariant();

            if (_context.Suppliers.Any(s => s.ContactInfo.ToLower() == email))
            {
                ModelState.AddModelError("", "This email is already registered as a supplier.");
                ViewBag.Departments = _context.Departments
                    .Select(d => new SelectListItem
                    {
                        Value = d.DepartmentID.ToString(),
                        Text = d.DepartmentName
                    })
                    .ToList();
                return View("Signup", model);
            }

            if (_context.Users.Any(u => u.Email.ToLower() == email))
            {
                ModelState.AddModelError("", "Email is already registered.");
                ViewBag.Departments = _context.Departments
                    .Select(d => new SelectListItem
                    {
                        Value = d.DepartmentID.ToString(),
                        Text = d.DepartmentName
                    })
                    .ToList();
                return View("Signup", model);
            }

            var dept = _context.Departments.FirstOrDefault(d => d.DepartmentID == model.DepartmentID);

            if (dept == null)
            {
                ModelState.AddModelError("", "Selected department does not exist.");
                ViewBag.Departments = _context.Departments
                    .Select(d => new SelectListItem
                    {
                        Value = d.DepartmentID.ToString(),
                        Text = d.DepartmentName
                    })
                    .ToList();
                return View("Signup", model);
            }

            var user = new User
            {
                FullName = model.FullName?.Trim(),
                Email = email,
                DepartmentID = dept.DepartmentID,
                RoleID = 1, // Default olarak Officer (1) role'ü veriyoruz
                IsActive = true
            };

            user.PasswordHash = _userPasswordHasher.HashPassword(user, model.Password);

            try
            {
                _context.Users.Add(user);
                _context.SaveChanges();
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred while creating the account.");
                ViewBag.Departments = _context.Departments
                    .Select(d => new SelectListItem
                    {
                        Value = d.DepartmentID.ToString(),
                        Text = d.DepartmentName
                    })
                    .ToList();
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
                ModelState.AddModelError("TaxId", "Tax ID is already registered.");
                return View("Signup", model);
            }

            if (_context.Users.Any(u => u.Email.ToLower() == email) || _context.Suppliers.Any(s => s.ContactInfo.ToLower() == email))
            {
                ModelState.AddModelError("ContactInfo", "This email is already registered.");
                return View("Signup", model);
            }

            if (!model.AcceptTerms)
            {
                ModelState.AddModelError("AcceptTerms", "You must accept the terms and conditions.");
                return View("Signup", model);
            }

            var supplier = new Supplier
            {
                SupplierName = model.BusinessName?.Trim(),
                TaxID = model.TaxId?.Trim(),
                ContactInfo = email,
            };

            supplier.PasswordHash = _supplierPasswordHasher.HashPassword(supplier, model.Password);

            try
            {
                _context.Suppliers.Add(supplier);
                _context.SaveChanges();
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred while creating the supplier account.");
                return View("Signup", model);
            }

            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim().ToLowerInvariant();
            var claims = new List<Claim>();
            ClaimsIdentity identity;

            // 1. Kullanıcı olarak giriş yapmayı dene
            var user = await _context.Users
                .Include(u => u.Role)
                .SingleOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user != null && user.IsActive)
            {
                var verificationResult = _userPasswordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
                if (verificationResult == PasswordVerificationResult.Success || verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    claims.AddRange(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                        new Claim(ClaimTypes.Name, user.FullName),
                        new Claim("UserType", "User"),
                        new Claim(ClaimTypes.Role, user.RoleID.ToString()),
                        new Claim("RoleName", user.Role?.RoleName ?? "Unknown")
                    });

                    identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync("CookieAuth", principal);

                    // Kullanıcının rolüne göre yönlendirme
                    return user.RoleID switch
                    {
                        1 => RedirectToAction("Profile", "Buyer"), // Officer
                        2 => RedirectToAction("Profile", "Buyer"), // Manager
                        3 => RedirectToAction("Profile", "Buyer"), // Director
                        4 => RedirectToAction("Profile", "Buyer"), // CFO
                        _ => RedirectToAction("Profile", "Buyer")  // Default
                    };
                }
            }

            // 2. Tedarikçi olarak giriş yapmayı dene
            var supplier = await _context.Suppliers.SingleOrDefaultAsync(s => s.ContactInfo.ToLower() == email);
            if (supplier != null)
            {
                var verificationResult = _supplierPasswordHasher.VerifyHashedPassword(supplier, supplier.PasswordHash, model.Password);
                if (verificationResult == PasswordVerificationResult.Success || verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    claims.AddRange(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, supplier.SupplierID.ToString()),
                        new Claim(ClaimTypes.Name, supplier.SupplierName),
                        new Claim("UserType", "Supplier")
                    });

                    identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync("CookieAuth", principal);

                    return RedirectToAction("Dashboard", "Supplier");
                }
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }

    }
}