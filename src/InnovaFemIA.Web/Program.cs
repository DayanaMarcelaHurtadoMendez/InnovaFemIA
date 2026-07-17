using InnovaFemIA.Web.BackgroundServices;
using InnovaFemIA.Web.Features.Shared.Middleware;
using InnovaFemIA.Web.Features.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
            mysqlOptions.CommandTimeout(30);
        }
    )
);

// ─── MVC + RAZOR VIEWS ────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ─── MEDIATOR (CQRS - Vertical Slice Architecture) ───────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<Program>());

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
builder.Services.AddHostedService<ScrapingPreciosService>();

// ─── HTTP CLIENT (para scraping) ──────────────────────────────────────────────
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
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();

// ─── MIDDLEWARE API KEY ────────────────────────────────────────────────────────
// IMPORTANTE: Debe ir DESPUÉS de UseRouting() para que el endpoint ya esté resuelto
// y el middleware pueda leer los metadatos (atributo [PublicApi]) del endpoint.
app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// ─── RUTAS ────────────────────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers(); // Para [ApiController] attribute routing

// ─── MIGRACIÓN AUTOMÁTICA Y SEED EN DEVELOPMENT ───────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Aplica migraciones pendientes automáticamente en desarrollo
        await db.Database.MigrateAsync();
        logger.LogInformation("Migraciones aplicadas correctamente.");

        // Seed del administrador por defecto si no existe
        await SeedAdminAsync(db, app.Configuration, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al aplicar migraciones o seed inicial.");
    }
}

app.Run();

// ─── SEED HELPERS ─────────────────────────────────────────────────────────────
static async Task SeedAdminAsync(
    AppDbContext db,
    IConfiguration config,
    ILogger logger)
{
    var usuario = config.GetValue<string>("Admin:UsuarioPorDefecto", "admin")!;
    var contrasena = config.GetValue<string>("Admin:ContrasenaPorDefecto", "CambiarEstaContrasena123!")!;
    var email = config.GetValue<string>("Admin:EmailPorDefecto", "admin@innovafemia.local")!;

    var admin = await db.Administradores.FirstOrDefaultAsync(a => a.NombreUsuario == usuario);
    if (admin == null)
    {
        db.Administradores.Add(new InnovaFemIA.Web.Features.Shared.Entities.Administrador
        {
            NombreUsuario = usuario,
            HashContrasena = BCrypt.Net.BCrypt.HashPassword(contrasena, workFactor: 12),
            NombreCompleto = "Administrador InnovaFemIA",
            Email = email,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });
        logger.LogInformation("Administrador por defecto creado: {Usuario}", usuario);
    }
    else
    {
        admin.Email = email;
        logger.LogInformation("Email de administrador actualizado a: {Email}", email);
    }
    await db.SaveChangesAsync();
}
