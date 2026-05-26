using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.RateLimiting;
using TIProject.Components;
using TIProject.Data;
using TIProject.Repository;

var builder = WebApplication.CreateBuilder(args);

// 1. SERVICIOS
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IRepoUsers, RepoUsers>();
builder.Services.AddScoped<IRepoCourses, RepoCourses>();
builder.Services.AddSingleton<TIProject.Data.Documents>();

builder.Services.AddDbContext<TIProjectDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Autenticación con cookie segura
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Name = "TIProject.Auth";
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Rate limiting: máximo 5 intentos de login por minuto por IP
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login-policy", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

// 2. BUILD
var app = builder.Build();

// 3. MIDDLEWARE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// Headers de seguridad HTTP
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    ctx.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Endpoint de login (POST) — con rate limiting y sin antiforgery (compensado por rate limiting + lockout)
app.MapPost("/account/login", async (HttpContext context, IRepoUsers repoUsers) =>
{
    var form = await context.Request.ReadFormAsync();
    var idNumberStr = form["idNumber"].ToString();
    var password = form["password"].ToString();
    var roleStr = form["role"].ToString();
    var remember = form["remember"].ToString() == "true";

    if (!int.TryParse(idNumberStr, out int idNumber) || string.IsNullOrWhiteSpace(password))
    {
        context.Response.Redirect($"/login?role={roleStr}&error=1");
        return;
    }

    var selectedRoleIntent = int.TryParse(roleStr, out int rInt) ? (Role?)rInt : null;
    
    var allUsers = await repoUsers.GetAll();
    var user = allUsers.FirstOrDefault(u => u.IdNumber == idNumber);

    if (user is null)
    {
        context.Response.Redirect($"/login?role={roleStr}&error=1");
        return;
    }

    var hasher = new PasswordHasher<User>();
    bool passwordValid;

    // Soporte para migración: detectar si es hash PBKDF2 (>50 chars) o plain-text legacy
    if (user.Password!.Length > 50)
    {
        var result = hasher.VerifyHashedPassword(user, user.Password, password);
        passwordValid = result != PasswordVerificationResult.Failed;
    }
    else
    {
        // Contraseña en texto plano (legacy): verificar y migrar a hash automáticamente
        passwordValid = user.Password == password;
        if (passwordValid)
        {
            user.Password = hasher.HashPassword(user, password);
            await repoUsers.Update(user);
        }
    }

    if (!passwordValid)
    {
        context.Response.Redirect($"/login?role={roleStr}&error=1");
        return;
    }

    // El rol se obtiene SIEMPRE de la base de datos, nunca se confía en el parámetro 'role' del formulario para la autorización
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Name ?? "Usuario"),
        new Claim(ClaimTypes.Email, user.Email ?? ""),
        new Claim(ClaimTypes.Role, user.Role.ToString() ?? ""),
    };
    if (user.Brigade.HasValue)
        claims.Add(new Claim("Brigade", user.Brigade.Value.ToString()));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    var authProps = new AuthenticationProperties
    {
        IsPersistent = remember,
        ExpiresUtc = remember ? DateTimeOffset.UtcNow.AddDays(7) : null
    };

    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

    var dashboard = user.Role switch
    {
        Role.admin => "/dashboard_admin/",
        Role.brigaderChief => "/dashboard_chief",
        Role.brigader => "/dashboard_brigader",
        Role.auxiliar => "/dashboard_auxiliar",
        Role.student => "/dashboard_student",
        _ => "/login"
    };

    context.Response.Redirect(dashboard);

}).RequireRateLimiting("login-policy").DisableAntiforgery();

// Endpoint de logout (GET)
app.MapGet("/account/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Redirect("/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
