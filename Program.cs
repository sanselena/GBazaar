// Ensures ProcurementContext is found
using GBazaar.Models;
using Gbazaar.Data;// Ensure Models are accessible if needed globally (Good practice)
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ==========================================================
// 1. SERVICES CONFIGURATION (builder.Services.Add...)
// ==========================================================

// Add MVC services
builder.Services.AddControllersWithViews();

//cookie auth
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Home/AccessDenied";

        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

builder.Services.AddHttpContextAccessor();

// --- DATABASE SETUP START ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Register your DbContext
builder.Services.AddDbContext<ProcurementContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


var app = builder.Build();

var defaultCulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

// ==========================================================
// 2. APPLICATION PIPELINE (app.Use...)
// ==========================================================

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Production/Staging only handlers:
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
// Note: The rest of the pipeline must be OUTSIDE the 'if' block.

app.UseHttpsRedirection();
app.UseStaticFiles(); // Essential for serving CSS, JS, etc.

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run(); // Starts the application