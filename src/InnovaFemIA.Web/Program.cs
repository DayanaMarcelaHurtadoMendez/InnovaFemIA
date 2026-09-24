using InnovaFemIA.Web.BackgroundServices;
using InnovaFemIA.Web.Features.Shared.Middleware;
using InnovaFemIA.Web.Features.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseWebRoot("wwwroot");

// ─── BASE DE DATOS ─────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mysqlOptions =>
        {
            mysqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
            mysqlOptions.CommandTimeout(300); // 5 minutos de timeout para subidas de videos pesados
            mysqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        }
    )
);

// ─── CONFIGURACIÓN DE TAMAÑO MÁXIMO DE CARGA DE ARCHIVOS (500 MB) ────────────
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500 MB
    options.ValueLengthLimit = 500 * 1024 * 1024;
    options.MultipartHeadersLengthLimit = 500 * 1024 * 1024;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024; // 500 MB
});

// ─── MVC + RAZOR VIEWS + SERVICIOS CORE ───────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<InnovaFemIA.Web.Features.Shared.Services.IMediaCacheService, InnovaFemIA.Web.Features.Shared.Services.MediaCacheService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// ─── SESIÓN (cookies para Admin y Familias) ───────────────────────────────────
var sesionExpiracion = builder.Configuration.GetValue<int>("Sesion:TiempoExpiracionMinutos", 60);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(sesionExpiracion);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// ─── AUTENTICACIÓN CON COOKIES ────────────────────────────────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "InnovaFemIA.Admin";
})
.AddCookie("InnovaFemIA.Admin", options =>
{
    options.LoginPath = "/admin/login";
    options.LogoutPath = "/admin/logout";
    options.AccessDeniedPath = "/admin/acceso-denegado";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(sesionExpiracion);
    options.SlidingExpiration = true;
    options.Cookie.Name = "InnovaFemIA.Admin";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
})
.AddCookie("InnovaFemIA.Familia", options =>
{
    options.LoginPath = "/biocomercio/login";
    options.LogoutPath = "/biocomercio/logout";
    options.AccessDeniedPath = "/biocomercio/acceso-denegado";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(sesionExpiracion);
    options.SlidingExpiration = true;
    options.Cookie.Name = "InnovaFemIA.Familia";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddAuthorization();

// ─── BACKGROUND SERVICE — SCRAPING DANE ──────────────────────────────────────
builder.Services.AddSingleton<ScrapingPreciosService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ScrapingPreciosService>());
builder.Services.AddHttpClient();

// ─── LOGGING ──────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ────────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ────────────────────────────────────────────────────────────────────────────

// ─── PIPELINE HTTP ────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// ─── LOCALIZACIÓN (Punto decimal para coordenadas GPS y números en formularios) ───
var defaultCulture = new System.Globalization.CultureInfo("es-CO");
defaultCulture.NumberFormat.NumberDecimalSeparator = ".";
defaultCulture.NumberFormat.NumberGroupSeparator = ",";
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture),
    SupportedCultures = [defaultCulture, System.Globalization.CultureInfo.InvariantCulture],
    SupportedUICultures = [defaultCulture, System.Globalization.CultureInfo.InvariantCulture]
});

// ─── ARCHIVOS ESTÁTICOS ROBUSTOS ──────────────────────────────────────────────
app.UseCaseInsensitiveStaticFiles();

app.UseRouting();

// ─── MIDDLEWARE API KEY ────────────────────────────────────────────────────────
app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// ─── RESTRICCIÓN MODO MAPA SOLO (SI ESTÁ ACTIVO EN EL CONTENEDOR) ───────────────
app.UseModoMapaSolo();

// ─── RUTAS & ENDPOINTS ────────────────────────────────────────────────────────
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

// ─── HEALTH CHECK (Docker / Kubernetes / Monitoreo) ───────────────────────────
app.MapGet("/health", () => Results.Ok(new { status = "healthy", app = "InnovaFemIA", timestamp = DateTime.UtcNow }));

// ─── INICIALIZACIÓN RESILIENTE DE BASE DE DATOS Y ADMIN ─────────────────────────
await app.InitializeDatabaseAsync();

app.Run();
