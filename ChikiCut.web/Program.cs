using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using ChikiCut.web.Data;
using ChikiCut.web.Services;
using ChikiCut.web.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("=== INICIO: App se está construyendo ===");

// Configurar timezone para PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers(); // Agregado para habilitar los endpoints de la API

builder.Services.Configure<ApiBehaviorOptions>(o =>
{
    // Quiero manejar yo mismo las respuestas cuando ModelState es inválido
    o.SuppressModelStateInvalidFilter = true;
});

// Configurar Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"), npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure();
    });
});

// Configurar servicios de autenticación
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<ISucursalFilterService, SucursalFilterService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IGastoService, GastoService>();

// Configurar helpers
builder.Services.AddScoped<PermissionHelper>();
builder.Services.AddHttpContextAccessor();

// Configurar sesiones
builder.Services.AddDistributedMemoryCache();
if (builder.Environment.IsDevelopment())
{
    // En desarrollo permitimos SameSite Lax y SameAsRequest para que funcione con HTTP local
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.Name = "ChikiCut.Session";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });
}
else
{
    // En producción forzamos Secure y SameSite=None para compatibilidad con HTTPS y cross-site navigation
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.Name = "ChikiCut.Session";
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
}

// Configurar autenticación por cookies
builder.Services.AddAuthentication("ChikiCutCookie")
    .AddCookie("ChikiCutCookie", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "ChikiCut.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Configurar localización
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("es-MX") };
    options.DefaultRequestCulture = new RequestCulture("es-MX");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var app = builder.Build();

Console.WriteLine("=== App construida correctamente ===");

// Middleware de diagnóstico para logging de requests y responses
app.Use(async (context, next) =>
{
    Console.WriteLine($"[{DateTime.Now}] {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
    await next();
    Console.WriteLine($"[{DateTime.Now}] Respondió {context.Response.StatusCode}");
});

// Middleware global para loguear excepciones
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{DateTime.Now}] Error global: {ex}");
        throw;
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseRequestLocalization();
app.UseSession();
app.UseAuthentication(); // <-- Agregado aquí
app.UseAuthorization();

// El orden correcto es MapRazorPages antes de MapControllers
app.MapRazorPages();
app.MapControllers();

// Listar endpoints registrados
var endpointDataSource = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
foreach (var ep in endpointDataSource.Endpoints)
{
    Console.WriteLine($"ENDPOINT: {ep.DisplayName}");
}

Console.WriteLine("=== Antes de app.Run() ===");

app.Run();

Console.WriteLine("=== Después de app.Run() (esto nunca debería verse) ===");
