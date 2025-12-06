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
        private readonly ILogger<AuthController> _logger;

        public AuthController(ProcurementContext context, ILogger<AuthController> logger)
        {
            _context = context;
            _logger = logger;
            _userPasswordHasher = new PasswordHasher<User>();
            _supplierPasswordHasher = new PasswordHasher<Supplier>();
        }

        public IActionResult Login() => View();

        public IActionResult Signup()
        {
            ViewBag.Departments = _context.Departments
                .Select(d => new SelectListItem
                {
                    Value = d.DepartmentID.ToString(),
                    Text = d.DepartmentName
                })
                .ToList();

            ViewBag.Roles = _context.Roles
                .Select(r => new SelectListItem
                {
                    Value = r.RoleID.ToString(),
                    Text = r.RoleName
                })
                .ToList();

            ViewBag.PaymentTerms = _context.PaymentTerms
                .Select(pt => new SelectListItem
                {
                    Value = pt.PaymentTermID.ToString(),
                    Text = pt.Description
                })
                .ToList();

            return View();
        }

        [HttpPost]
        public IActionResult SignupBuyer(SignupBuyerVM model)
        {
            if (!ModelState.IsValid)
            {
                ResetViewBags();
                return View("Signup", model);
            }

            var email = model.Email.Trim().ToLowerInvariant();

            // Email kontrolü
            if (_context.Suppliers.Any(s => s.ContactInfo.ToLower() == email))
            {
                ModelState.AddModelError("", "This email is already registered as a supplier.");
                ResetViewBags();
                return View("Signup", model);
            }

            if (_context.Users.Any(u => u.Email.ToLower() == email))
            {
                ModelState.AddModelError("", "Email is already registered.");
                ResetViewBags();
                return View("Signup", model);
            }

            // Department kontrolü
            var dept = _context.Departments.FirstOrDefault(d => d.DepartmentID == model.DepartmentID);
            if (dept == null)
            {
                ModelState.AddModelError("", "Selected department does not exist.");
                ResetViewBags();
                return View("Signup", model);
            }

            // Role kontrolü
            var role = _context.Roles.FirstOrDefault(r => r.RoleID == model.RoleID);
            if (role == null)
            {
                ModelState.AddModelError("", "Selected role does not exist.");
                ResetViewBags();
                return View("Signup", model);
            }

            // Role uniqueness kontrolü (Officer hariç)
            if (model.RoleID != 1) // Officer değilse
            {
                var existingUserWithRole = _context.Users
                    .FirstOrDefault(u => u.DepartmentID == model.DepartmentID &&
                                        u.RoleID == model.RoleID &&
                                        u.IsActive);

                if (existingUserWithRole != null)
                {
                    var roleNames = new Dictionary<int, string>
                    {
                        { 2, "Manager" },
                        { 3, "Director" },
                        { 4, "CFO" }
                    };

                    var roleName = roleNames.GetValueOrDefault(model.RoleID, "this role");
                    ModelState.AddModelError("", $"This department already has a registered {roleName}. Only one {roleName} is allowed per department.");
                    ResetViewBags();
                    return View("Signup", model);
                }
            }

            // User oluştur
            var user = new User
            {
                FullName = model.FullName?.Trim(),
                Email = email,
                DepartmentID = dept.DepartmentID,
                RoleID = model.RoleID,
                IsActive = true
            };

            user.PasswordHash = _userPasswordHasher.HashPassword(user, model.Password);

            try
            {
                _context.Users.Add(user);
                _context.SaveChanges();

                _logger.LogInformation("New buyer account created: {Email} as {RoleName} in {DepartmentName}",
                    email, role.RoleName, dept.DepartmentName);

                TempData["SignupSuccess"] = $"Account created successfully as {role.RoleName}! You can now log in.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create buyer account for {Email}", email);
                ModelState.AddModelError("", "An unexpected error occurred while creating the account.");
                ResetViewBags();
                return View("Signup", model);
            }
        }

        [HttpPost]
        public IActionResult SignupSupplier(SignupSupplierVM model)
        {
            if (!ModelState.IsValid)
            {
                ResetViewBags();
                return View("Signup", model);
            }

            var email = model.ContactInfo.Trim().ToLowerInvariant();

            // Email kontrolü
            if (_context.Users.Any(u => u.Email.ToLower() == email))
            {
                ModelState.AddModelError("", "This email is already registered as a buyer.");
                ResetViewBags();
                return View("Signup", model);
            }

            if (_context.Suppliers.Any(s => s.ContactInfo.ToLower() == email))
            {
                ModelState.AddModelError("", "Email is already registered as a supplier.");
                ResetViewBags();
                return View("Signup", model);
            }

            // Tax ID kontrolü (eğer girilmişse)
            if (!string.IsNullOrWhiteSpace(model.TaxId) &&
                _context.Suppliers.Any(s => s.TaxID == model.TaxId.Trim()))
            {
                ModelState.AddModelError("TaxId", "Tax ID is already registered.");
                ResetViewBags();
                return View("Signup", model);
            }

            // Payment Term kontrolü
            var paymentTerm = _context.PaymentTerms.FirstOrDefault(pt => pt.PaymentTermID == model.PaymentTermID);
            if (paymentTerm == null)
            {
                ModelState.AddModelError("", "Selected payment term does not exist.");
                ResetViewBags();
                return View("Signup", model);
            }

            if (!model.AcceptTerms)
            {
                ModelState.AddModelError("AcceptTerms", "You must accept the terms and conditions.");
                ResetViewBags();
                return View("Signup", model);
            }

            // Supplier oluştur
            var supplier = new Supplier
            {
                SupplierName = model.BusinessName.Trim(),
                ContactName = model.ContactName?.Trim(),
                ContactInfo = email,
                TaxID = model.TaxId?.Trim(),
                PaymentTermID = model.PaymentTermID
            };

            supplier.PasswordHash = _supplierPasswordHasher.HashPassword(supplier, model.Password);

            try
            {
                _context.Suppliers.Add(supplier);
                _context.SaveChanges();

                _logger.LogInformation("New supplier account created: {BusinessName} ({Email})",
                    supplier.SupplierName, email);

                TempData["SignupSuccess"] = $"Supplier account created successfully for '{supplier.SupplierName}'! You can now log in.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create supplier account for {BusinessName}", model.BusinessName);
                ModelState.AddModelError("", "An unexpected error occurred while creating the supplier account.");
                ResetViewBags();
                return View("Signup", model);
            }
        }

        private void ResetViewBags()
        {
            ViewBag.Departments = _context.Departments
                .Select(d => new SelectListItem
                {
                    Value = d.DepartmentID.ToString(),
                    Text = d.DepartmentName
                })
                .ToList();

            ViewBag.Roles = _context.Roles
                .Select(r => new SelectListItem
                {
                    Value = r.RoleID.ToString(),
                    Text = r.RoleName
                })
                .ToList();

            ViewBag.PaymentTerms = _context.PaymentTerms
                .Select(pt => new SelectListItem
                {
                    Value = pt.PaymentTermID.ToString(),
                    Text = pt.Description
                })
                .ToList();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim().ToLowerInvariant();

            try
            {
                // 1. Kullanıcı olarak giriş yapmayı dene
                var user = await _context.Users
                    .Include(u => u.Role)
                    .SingleOrDefaultAsync(u => u.Email.ToLower() == email);

                if (user != null && user.IsActive)
                {
                    var verificationResult = _userPasswordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

                    if (verificationResult == PasswordVerificationResult.Success ||
                        verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        var claims = new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                            new Claim(ClaimTypes.Name, user.FullName ?? "Unknown"),
                            new Claim("UserType", "User"),
                            new Claim(ClaimTypes.Role, user.RoleID.ToString()),
                            new Claim("RoleName", user.Role?.RoleName ?? "Unknown")
                        };

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);
                        await HttpContext.SignInAsync("CookieAuth", principal);

                        // Update last login time
                        user.LastLoginAt = DateTime.UtcNow;
                        _context.Users.Update(user);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("User {Email} ({RoleName}) logged in successfully",
                            email, user.Role?.RoleName ?? "Unknown");

                        // Kullanıcının rolüne göre yönlendirme
                        return user.Role?.RoleName?.ToLowerInvariant() switch
                        {
                            "admin" => RedirectToAction("Dashboard", "Admin"),
                            "officer" => RedirectToAction("Profile", "Buyer"),
                            "manager" => RedirectToAction("Profile", "Buyer"),
                            "director" => RedirectToAction("Profile", "Buyer"),
                            "cfo" => RedirectToAction("Profile", "Buyer"),
                            _ => RedirectToAction("Profile", "Buyer")
                        };
                    }
                }

                // 2. Tedarikçi olarak giriş yapmayı dene
                var supplier = await _context.Suppliers.SingleOrDefaultAsync(s => s.ContactInfo.ToLower() == email);
                if (supplier != null)
                {
                    var verificationResult = _supplierPasswordHasher.VerifyHashedPassword(supplier, supplier.PasswordHash, model.Password);
                    if (verificationResult == PasswordVerificationResult.Success ||
                        verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        var claims = new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, supplier.SupplierID.ToString()),
                            new Claim(ClaimTypes.Name, supplier.SupplierName),
                            new Claim("UserType", "Supplier")
                        };

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);
                        await HttpContext.SignInAsync("CookieAuth", principal);

                        _logger.LogInformation("Supplier {BusinessName} ({Email}) logged in successfully",
                            supplier.SupplierName, email);

                        return RedirectToAction("Dashboard", "Supplier");
                    }
                }

                _logger.LogWarning("Failed login attempt for email: {Email}", email);
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", email);
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            var userType = User.FindFirst("UserType")?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;

            await HttpContext.SignOutAsync("CookieAuth");

            _logger.LogInformation("User logged out: {UserName} ({UserType})", userName, userType);

            return RedirectToAction("Login");
        }
    }
}