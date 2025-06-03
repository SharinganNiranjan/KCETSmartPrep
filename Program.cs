using KcetPrep1.Data;
using KcetPrep1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// 1) DETERMINE THE CONNECTION STRING BASED ON ENVIRONMENT
// ─────────────────────────────────────────────────────────────────────────────
string sqlConnString;

if (builder.Environment.IsDevelopment())
{
    // In Development, read from appsettings.json → "DefaultConnection"
    sqlConnString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(sqlConnString))
    {
        throw new Exception("DefaultConnection is not set in appsettings.json.");
    }
}
else
{
    // In Production (Railway), read the MSSQL_CONNECTION_STRING or SQLSERVER_URL env var
    sqlConnString =
        Environment.GetEnvironmentVariable("MSSQL_CONNECTION_STRING")
        ?? Environment.GetEnvironmentVariable("SQLSERVER_URL");

    if (string.IsNullOrEmpty(sqlConnString))
    {
        throw new Exception(
            "MSSQL_CONNECTION_STRING (or SQLSERVER_URL) is not set. " +
            "Did you add the Railway SQL Server plugin and copy its connection string name exactly?");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 2) REGISTER DbContext USING THE SELECTED CONNECTION STRING
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(sqlConnString));

// ─────────────────────────────────────────────────────────────────────────────
// 3) CONFIGURE ASP.NET IDENTITY WITH ROLES
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ─────────────────────────────────────────────────────────────────────────────
// 4) MVC + RAZOR RUNTIME COMPILATION
// ─────────────────────────────────────────────────────────────────────────────
builder.Services
    .AddControllersWithViews(options =>
    {
        options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
    })
    .AddRazorRuntimeCompilation();

// ─────────────────────────────────────────────────────────────────────────────
// 5) SESSION CONFIGURATION
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ─────────────────────────────────────────────────────────────────────────────
// 6) DEPENDENCY INJECTION FOR YOUR DATA SERVICE
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<DataService>();

// ─────────────────────────────────────────────────────────────────────────────
// 7) LOGGING
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});

// ─────────────────────────────────────────────────────────────────────────────
// 8) CONFIGURE COOKIE PATHS FOR AUTHENTICATION
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LogoutPath = "/Account/Logout";
});

// ─────────────────────────────────────────────────────────────────────────────
// 9) BIND KESTREL TO RAILWAY’S PROVIDED PORT (OR DEFAULT TO :80)
// ─────────────────────────────────────────────────────────────────────────────
var port = Environment.GetEnvironmentVariable("PORT") ?? "80";
builder.WebHost.UseUrls($"http://*:{port}");

var app = builder.Build();

// ─────────────────────────────────────────────────────────────────────────────
// 10) RUN EF MIGRATIONS + SEED ROLES, ADMIN, AND (OPTIONAL) CSV DATA
// ─────────────────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // 10a) APPLY ANY PENDING MIGRATIONS
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        // 10b) SEED ROLES AND ADMIN USER
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = { "Admin", "Student" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminEmail = "admin@kcetprep.com";
        var adminPassword = "Admin@123";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Admin User"
            };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // 10c) OPTIONAL CSV SEEDING (ONLY IF THE FILE EXISTS)
        //    - In appsettings.json you have "CsvDataPath": "C:\\Users\\hirem\\…"
        //    - On Railway, that Windows-style path won’t exist, so the code skips it.
        var dataService = services.GetRequiredService<DataService>();

        // Read CsvDataPath from configuration (appsettings.json or env var override)
        var csvDataPath = builder.Configuration["CsvDataPath"]
                          // If you want to override from Railway Variables, set CSVDATAPATH env var.
                          ?? string.Empty;

        if (!string.IsNullOrEmpty(csvDataPath) && File.Exists(csvDataPath))
        {
            await dataService.SeedDatabaseFromCsvAsync(csvDataPath);
        }
        else
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation(
                "CSV file not found at '{CsvDataPath}'. Skipping CSV seed.",
                csvDataPath);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ An error occurred while seeding the database.");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 11) MIDDLEWARE PIPELINE (UNCHANGED)
// ─────────────────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Default route points to Account/Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
