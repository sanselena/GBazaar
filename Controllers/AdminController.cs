using Gbazaar.Data;
using GBazaar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GBazaar.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ProcurementContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ProcurementContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            // checkadmin
            var roleName = User.FindFirst("RoleName")?.Value;
            if (roleName?.ToLowerInvariant() != "admin")
            {
                return Forbid();
            }

            ViewBag.UserType = "Admin";
            
            try
            {
                // admin content
                var stats = new
                {
                    TotalUsers = await _context.Users.CountAsync(),
                    ActiveUsers = await _context.Users.CountAsync(u => u.IsActive),
                    TotalSuppliers = await _context.Suppliers.CountAsync(),
                    TotalProducts = await _context.Products.CountAsync(),
                    PendingRequests = await _context.PurchaseRequests
                        .CountAsync(pr => pr.PRStatus == Models.Enums.PRStatusType.PendingApproval),
                    TotalPurchaseOrders = await _context.PurchaseOrders.CountAsync()
                };

                // recent activity
                var recentUsers = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Department)
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                var recentSuppliers = await _context.Suppliers
                    .OrderByDescending(s => s.SupplierID)
                    .Take(5)
                    .ToListAsync();

                ViewBag.Stats = stats;
                ViewBag.RecentUsers = recentUsers;
                ViewBag.RecentSuppliers = recentSuppliers;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["AdminError"] = "Failed to load dashboard data.";
                return View();
            }
        }

        public async Task<IActionResult> UserManagement()
        {
            var roleName = User.FindFirst("RoleName")?.Value;
            if (roleName?.ToLowerInvariant() != "admin")
            {
                return Forbid();
            }

            ViewBag.UserType = "Admin";

            try
            {
                var users = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Department)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user management data");
                TempData["AdminError"] = "Failed to load user data.";
                return View(new List<User>());
            }
        }

        public async Task<IActionResult> SupplierManagement()
        {
            var roleName = User.FindFirst("RoleName")?.Value;
            if (roleName?.ToLowerInvariant() != "admin")
            {
                return Forbid();
            }

            ViewBag.UserType = "Admin";

            var suppliers = await _context.Suppliers
                .Include(s => s.PaymentTerm)
                .OrderBy(s => s.SupplierName)
                .ToListAsync();

            return View(suppliers);
        }

        public async Task<IActionResult> ProductManagement()
        {
            var roleName = User.FindFirst("RoleName")?.Value;
            if (roleName?.ToLowerInvariant() != "admin")
            {
                return Forbid();
            }

            ViewBag.UserType = "Admin";

            try
            {
                // product and relevant info
                var products = await _context.Products
                    .Include(p => p.Supplier)
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();

                // dep and  budget rel. info
                var departments = await _context.Departments
                    .Include(d => d.Users)
                    .Include(d => d.Budgets)
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync();

                ViewBag.Products = products;
                ViewBag.Departments = departments;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product and department management data");
                TempData["AdminError"] = "Failed to load data.";
                ViewBag.Products = new List<Product>();
                ViewBag.Departments = new List<Department>();
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int userId)
        {
            var roleName = User.FindFirst("RoleName")?.Value;
            if (roleName?.ToLowerInvariant() != "admin")
            {
                return Forbid();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();
                
                TempData["AdminSuccess"] = $"User {user.FullName} has been {(user.IsActive ? "activated" : "deactivated")}.";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(string FullName, string Email, string Password, string ConfirmPassword, int RoleID, int? DepartmentID)
        {
            var roleName = User.FindFirst("RoleName")?.Value;
            if (roleName?.ToLowerInvariant() != "admin")
            {
                return Forbid();
            }

            try
            {
                // validation
                if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
                {
                    TempData["AdminError"] = "Please fill all required fields.";
                    return RedirectToAction(nameof(UserManagement));
                }

                if (Password != ConfirmPassword)
                {
                    TempData["AdminError"] = "Passwords do not match.";
                    return RedirectToAction(nameof(UserManagement));
                }

                var email = Email.Trim().ToLowerInvariant();

                // emailusercheck
                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
                {
                    TempData["AdminError"] = "This email is already registered.";
                    return RedirectToAction(nameof(UserManagement));
                }

                // emailsupcheck
                if (await _context.Suppliers.AnyAsync(s => s.ContactInfo.ToLower() == email))
                {
                    TempData["AdminError"] = "This email is already registered as a supplier.";
                    return RedirectToAction(nameof(UserManagement));
                }

                // create new user
                var user = new User
                {
                    FullName = FullName.Trim(),
                    Email = email,
                    RoleID = RoleID,
                    DepartmentID = DepartmentID,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // passhash
                var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
                user.PasswordHash = passwordHasher.HashPassword(user, Password);

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["AdminSuccess"] = $"User '{FullName}' created successfully!";
                return RedirectToAction(nameof(UserManagement));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                TempData["AdminError"] = "An error occurred while creating the user.";
                return RedirectToAction(nameof(UserManagement));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _context.Roles
                    .Select(r => new { r.RoleID, r.RoleName })
                    .ToListAsync();
                return Json(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading roles");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                var departments = await _context.Departments
                    .Select(d => new { d.DepartmentID, d.DepartmentName })
                    .ToListAsync();
                return Json(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading departments");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSuppliers()
        {
            try
            {
                var suppliers = await _context.Suppliers
                    .Select(s => new { s.SupplierID, s.SupplierName })
                    .OrderBy(s => s.SupplierName)
                    .ToListAsync();
                return Json(suppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading suppliers");
                return Json(new List<object>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(string ProductName, string Description, int SupplierID, decimal? UnitPrice, string UnitOfMeasure)
        {
            var roleName = User.FindFirst("RoleName")?.Value;
            if (roleName?.ToLowerInvariant() != "admin")
            {
                return Forbid();
            }

            try
            {
                // validation
                if (string.IsNullOrWhiteSpace(ProductName) || SupplierID <= 0)
                {
                    TempData["AdminError"] = "Please fill all required fields.";
                    return RedirectToAction(nameof(ProductManagement));
                }

                // sup check
                var supplier = await _context.Suppliers.FindAsync(SupplierID);
                if (supplier == null)
                {
                    TempData["AdminError"] = "Selected supplier does not exist.";
                    return RedirectToAction(nameof(ProductManagement));
                }

                // product-sup match check
                if (await _context.Products.AnyAsync(p => p.SupplierID == SupplierID && p.ProductName.ToLower() == ProductName.ToLower().Trim()))
                {
                    TempData["AdminError"] = "This product name already exists for the selected supplier.";
                    return RedirectToAction(nameof(ProductManagement));
                }

                // create new product
                var product = new Product
                {
                    ProductName = ProductName.Trim(),
                    Description = Description?.Trim(),
                    SupplierID = SupplierID,
                    UnitPrice = UnitPrice,
                    UnitOfMeasure = UnitOfMeasure?.Trim()
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                TempData["AdminSuccess"] = $"Product '{ProductName}' created successfully for supplier '{supplier.SupplierName}'!";
                return RedirectToAction(nameof(ProductManagement));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                TempData["AdminError"] = "An error occurred while creating the product.";
                return RedirectToAction(nameof(ProductManagement));
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleProductStatus(int productId)
        {
            var roleName = User.FindFirst("RoleName")?.Value;
            if (roleName?.ToLowerInvariant() != "admin")
            {
                return Forbid();
            }

            try
            {
                var product = await _context.Products
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.ProductID == productId);
                
                if (product != null)
                {
                  
                    // check pro.dependencies
                    var hasPRItems = await _context.PRItems.AnyAsync(pri => pri.ProductID == productId);
                    var hasPOItems = await _context.POItems.AnyAsync(poi => poi.ProductID == productId);

                    if (hasPRItems || hasPOItems)
                    {
                        TempData["AdminError"] = $"Cannot delete product '{product.ProductName}' because it has existing purchase requests or orders.";
                    }
                    else
                    {
                        _context.Products.Remove(product);
                        await _context.SaveChangesAsync();
                        TempData["AdminSuccess"] = $"Product '{product.ProductName}' has been deleted successfully.";
                    }
                }
                else
                {
                    TempData["AdminError"] = "Product not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", productId);
                TempData["AdminError"] = "An error occurred while deleting the product.";
            }

            return RedirectToAction(nameof(ProductManagement));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProduct(int ProductID, string ProductName, string Description, int SupplierID, decimal? UnitPrice, string UnitOfMeasure)
        {
            var roleName = User.FindFirst("RoleName")?.Value;
            if (roleName?.ToLowerInvariant() != "admin")
            {
                return Forbid();
            }

            try
            {
                var product = await _context.Products.FindAsync(ProductID);
                if (product == null)
                {
                    TempData["AdminError"] = "Product not found.";
                    return RedirectToAction(nameof(ProductManagement));
                }

                // validation
                if (string.IsNullOrWhiteSpace(ProductName) || SupplierID <= 0)
                {
                    TempData["AdminError"] = "Please fill all required fields.";
                    return RedirectToAction(nameof(ProductManagement));
                }

                // sup check
                var supplier = await _context.Suppliers.FindAsync(SupplierID);
                if (supplier == null)
                {
                    TempData["AdminError"] = "Selected supplier does not exist.";
                    return RedirectToAction(nameof(ProductManagement));
                }

                // prodcheck
                if (await _context.Products.AnyAsync(p => p.ProductID != ProductID && p.SupplierID == SupplierID && p.ProductName.ToLower() == ProductName.ToLower().Trim()))
                {
                    TempData["AdminError"] = "This product name already exists for the selected supplier.";
                    return RedirectToAction(nameof(ProductManagement));
                }

                // update product
                product.ProductName = ProductName.Trim();
                product.Description = Description?.Trim();
                product.SupplierID = SupplierID;
                product.UnitPrice = UnitPrice;
                product.UnitOfMeasure = UnitOfMeasure?.Trim();

                await _context.SaveChangesAsync();

                TempData["AdminSuccess"] = $"Product '{ProductName}' updated successfully!";
                return RedirectToAction(nameof(ProductManagement));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                TempData["AdminError"] = "An error occurred while updating the product.";
                return RedirectToAction(nameof(ProductManagement));
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct([FromBody] DeleteProductRequest request)
        {
            var roleName = User.FindFirst("RoleName")?.Value;
            if (roleName?.ToLowerInvariant() != "admin")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var product = await _context.Products.FindAsync(request.Id);
                if (product != null)
                {
                    // check if product has any dependencies
                    var hasPRItems = await _context.PRItems.AnyAsync(pri => pri.ProductID == request.Id);
                    var hasPOItems = await _context.POItems.AnyAsync(poi => poi.ProductID == request.Id);

                    if (hasPRItems || hasPOItems)
                    {
                        return Json(new { success = false, message = "Cannot delete product with existing purchase requests or orders." });
                    }

                    _context.Products.Remove(product);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Product not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", request.Id);
                return Json(new { success = false, message = "Failed to delete product." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateDepartment(string DepartmentName, string Description, decimal BudgetAmount, string BudgetCode)
        {
            var roleName = User.FindFirst("RoleName")?.Value;
            if (roleName?.ToLowerInvariant() != "admin")
            {
                return Forbid();
            }

            try
            {
                // validation
                if (string.IsNullOrWhiteSpace(DepartmentName) || string.IsNullOrWhiteSpace(BudgetCode))
                {
                    TempData["AdminError"] = "Please fill all required fields.";
                    return RedirectToAction(nameof(ProductManagement));
                }

                // dep check
                if (await _context.Departments.AnyAsync(d => d.DepartmentName.ToLower() == DepartmentName.ToLower().Trim()))
                {
                    TempData["AdminError"] = "Department name already exists.";
                    return RedirectToAction(nameof(ProductManagement));
                }

                // b.code check
                if (await _context.Departments.AnyAsync(d => d.BudgetCode.ToLower() == BudgetCode.ToLower().Trim()))
                {
                    TempData["AdminError"] = "Budget code already exists.";
                    return RedirectToAction(nameof(ProductManagement));
                }

                // create new dep
                var department = new Department
                {
                    DepartmentName = DepartmentName.Trim(),
                    BudgetCode = BudgetCode.Trim()
                };

                _context.Departments.Add(department);
                await _context.SaveChangesAsync();

                // create budget for dep
                var budget = new Budget
                {
                    DepartmentID = department.DepartmentID,
                    TotalBudget = BudgetAmount,
                    AmountCommitted = 0,
                    FiscalYear = DateTime.Now.Year
                };

                _context.Budgets.Add(budget);
                await _context.SaveChangesAsync();

                TempData["AdminSuccess"] = $"Department '{DepartmentName}' created successfully with budget of {BudgetAmount:C}!";
                return RedirectToAction(nameof(ProductManagement));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department");
                TempData["AdminError"] = "An error occurred while creating the department.";
                return RedirectToAction(nameof(ProductManagement));
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDepartment([FromBody] DeleteDepartmentRequest request)
        {
            var roleName = User.FindFirst("RoleName")?.Value;
            if (roleName?.ToLowerInvariant() != "admin")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var department = await _context.Departments
                    .Include(d => d.Users)
                    .Include(d => d.Budgets)
                    .FirstOrDefaultAsync(d => d.DepartmentID == request.Id);

                if (department != null)
                {
                    // chechk users in the dep
                    if (department.Users.Any())
                    {
                        return Json(new { success = false, message = "Cannot delete department with assigned users. Please reassign users first." });
                    }

                    // del budget first
                    _context.Budgets.RemoveRange(department.Budgets);
                    
                    // del dep
                    _context.Departments.Remove(department);
                    await _context.SaveChangesAsync();
                    
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Department not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting department {DepartmentId}", request.Id);
                return Json(new { success = false, message = "Failed to delete department." });
            }
        }

        // Request classes for API endpoints
        public class DeleteProductRequest
        {
            public int Id { get; set; }
        }

        public class DeleteDepartmentRequest
        {
            public int Id { get; set; }
        }
    }
}
