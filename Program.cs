using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Hubs;
using PetWAMS.Web.Services;

var builder = WebApplication.CreateBuilder(args);


// ============================================================
// MVC
// ============================================================
builder.Services.AddControllersWithViews();


// ============================================================
// DATABASE - SQL SERVER
// ============================================================
builder.Services.AddDbContext<PetWamsDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("PetWamsDb")
    );
});


// ============================================================
// SIGNALR
// ============================================================
builder.Services.AddSignalR();


// ============================================================
// HTTP CLIENT
//
// Required for:
// - SSLCommerz Payment API
// - SSLCommerz Refund API
// ============================================================
builder.Services.AddHttpClient();


// ============================================================
// ML.NET PET RECOMMENDATION SERVICE
// ============================================================
builder.Services.AddScoped<MlPetRecommendationService>();


// ============================================================
// NATURAL LANGUAGE PREFERENCE SERVICE
//
// Example:
// "I need a dog. 1 year. male. labador"
//
// Parser will detect:
// Dog + 1 Year + Male + Labrador
// ============================================================
builder.Services.AddScoped<NaturalLanguagePreferenceService>();


// ============================================================
// NID DATA PROTECTION SERVICE
//
// Protects adopter NID numbers before database storage.
// ============================================================
builder.Services.AddDataProtection();

builder.Services.AddScoped<
    INidProtectionService,
    NidProtectionService>();


// ============================================================
// SSLCOMMERZ PAYMENT + REFUND SERVICE
// ============================================================
builder.Services.AddScoped<SslCommerzService>();


// ============================================================
// SESSION
// ============================================================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    // Session will remain active for 2 hours
    options.IdleTimeout = TimeSpan.FromHours(2);

    // Prevents client-side JavaScript from reading the cookie
    options.Cookie.HttpOnly = true;

    // Session cookie is essential for the application
    options.Cookie.IsEssential = true;
});


// ============================================================
// BUILD APPLICATION
// ============================================================
var app = builder.Build();


// ============================================================
// ERROR HANDLING
// ============================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


// ============================================================
// HTTPS
// ============================================================
app.UseHttpsRedirection();


// ============================================================
// STATIC FILES
//
// Allows:
// CSS
// JavaScript
// Images
// ============================================================
app.UseStaticFiles();


// ============================================================
// ROUTING
// ============================================================
app.UseRouting();


// ============================================================
// SESSION
//
// IMPORTANT:
// UseSession must be before controller route execution.
// ============================================================
app.UseSession();


// ============================================================
// SIGNALR NOTIFICATION HUB
// ============================================================
app.MapHub<NotificationHub>(
    "/notificationHub"
);


// ============================================================
// AREA ROUTE
//
// Examples:
//
// /Admin/Dashboard
// /Shelter/Dashboard
// /Adopter/Dashboard
// ============================================================
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
);


// ============================================================
// DEFAULT ROUTE
//
// Examples:
//
// /Home/Index
// /Account/SignIn
// ============================================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);


// ============================================================
// RUN APPLICATION
// ============================================================
app.Run();