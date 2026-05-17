using MedicalPractice.Data;
using MedicalPractice.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Services & Controllers ───────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// ── Authentication ───────────────────────────────────────────────────────────
// KEY FIX: SlidingExpiration is true, but the cookie MUST also be persistent
// (IsPersistent = true in SignInAsync) otherwise it is treated as a session cookie
// and is discarded when the browser closes — regardless of ExpireTimeSpan.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.LogoutPath = "/Account/Logout";

        // How long a "normal" login lasts.
        // "Remember Me" logins are extended to 7 days inside SignInEmployeeAsync.
        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        // Resets the expiry on each request so active users are never interrupted.
        options.SlidingExpiration = true;

        options.ReturnUrlParameter = "returnUrl";

        options.Cookie.HttpOnly = true;

        // SameAsRequest works in both HTTP (dev) and HTTPS (prod).
        // Always in prod-only scenarios, but breaks local HTTP development.
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        options.Cookie.SameSite = SameSiteMode.Lax;

        // Ensures the cookie is sent even if the user hasn't consented to
        // non-essential cookies (required for auth to survive browser restarts).
        options.Cookie.IsEssential = true;
    });

var app = builder.Build();

// ── Middleware pipeline ──────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Authentication MUST come before Authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();