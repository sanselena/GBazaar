using Gbazaar.Data;
using GBazaar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GBazaar.Controllers
{
    public class AdminController : Controller
    {
        private readonly ProcurementContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ProcurementContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Dashboard - Main hub
        public IActionResult Dashboard()
        {
            ViewBag.UserType = "Admin";
            return View();
        }

        // GET: Admin/UserManagement
        public async Task<IActionResult> UserManagement()
        {
            ViewBag.UserType = "Admin";
            var users = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Department)
                .OrderBy(u => u.UserID)
                .ToListAsync();
            return View(users);
        }

        // POST: Admin/DeleteUser
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/SupplierManagement
        public async Task<IActionResult> SupplierManagement()
        {
            ViewBag.UserType = "Admin";
            var suppliers = await _context.Suppliers
                .Include(s => s.PaymentTerm)
                .OrderBy(s => s.SupplierID)
                .ToListAsync();
            return View(suppliers);
        }

        // POST: Admin/DeleteSupplier
        [HttpPost]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);
                if (supplier == null)
                {
                    return Json(new { success = false, message = "Supplier not found" });
                }

                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Supplier deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/ProductDepartment
        public async Task<IActionResult> ProductDepartment()
        {
            ViewBag.UserType = "Admin";
            var products = await _context.Products
                .Include(p => p.Supplier)
                .OrderBy(p => p.ProductID)
                .ToListAsync();
            var departments = await _context.Departments
                .Include(d => d.Manager)
                .Include(d => d.Budgets)
                .OrderBy(d => d.DepartmentID)
                .ToListAsync();
            
            ViewBag.Products = products;
            ViewBag.Departments = departments;
            return View();
        }

        // POST: Admin/DeleteProduct
        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Product deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/CreateDepartment
        [HttpPost]
        public async Task<IActionResult> CreateDepartment([FromBody] Department department)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid data" });
                }

                _context.Departments.Add(department);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Department created successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/DeleteDepartment
        [HttpPost]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                {
                    return Json(new { success = false, message = "Department not found" });
                }

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Department deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/BudgetReports
        public IActionResult BudgetReports(int? year)
        {
            ViewBag.UserType = "Admin";
            return View();
        }
    }
}
