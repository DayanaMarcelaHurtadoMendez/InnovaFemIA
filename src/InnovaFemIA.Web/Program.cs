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

// ─── MVC + RAZOR VIEWS ────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ─── SERVICIOS DE ALTO RENDIMIENTO (CACHÉ LOCAL DE MEDIOS) ───────────────────
builder.Services.AddSingleton<InnovaFemIA.Web.Features.Shared.Services.IMediaCacheService, InnovaFemIA.Web.Features.Shared.Services.MediaCacheService>();

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
builder.Services.AddSingleton<ScrapingPreciosService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ScrapingPreciosService>());

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

// ─── LOCALIZACIÓN (Punto decimal para coordenadas GPS y números en formularios) ───
var defaultCulture = new System.Globalization.CultureInfo("es-CO");
defaultCulture.NumberFormat.NumberDecimalSeparator = ".";
defaultCulture.NumberFormat.NumberGroupSeparator = ",";
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture),
    SupportedCultures = [defaultCulture, System.Globalization.CultureInfo.InvariantCulture],
    SupportedUICultures = [defaultCulture, System.Globalization.CultureInfo.InvariantCulture]
};
app.UseRequestLocalization(localizationOptions);

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

// ─── MIGRACIÓN AUTOMÁTICA Y SEED ───────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Configura max_allowed_packet del servidor MySQL a 512 MB para soportar archivos de video pesados en LONGBLOB
        try
        {
            await db.Database.ExecuteSqlRawAsync("SET GLOBAL max_allowed_packet = 536870912;");
            await db.Database.CloseConnectionAsync();
            logger.LogInformation("MySQL max_allowed_packet configurado globalmente a 512 MB y pool de conexiones renovado.");
        }
        catch (Exception ex)
        {
            logger.LogWarning("No se pudo ejecutar SET GLOBAL max_allowed_packet: {Msg}", ex.Message);
        }

        // Aplica migraciones pendientes automáticamente
        await db.Database.MigrateAsync();
        logger.LogInformation("Migraciones aplicadas correctamente.");

        // Migrar automáticamente nombres de temas del chatbot si existen registros antiguos en BD
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                UPDATE ContenidosChatbot SET Tema = 'Siembra' WHERE Tema = 'CuandoSembrar';
                UPDATE ContenidosChatbot SET Tema = 'Cosecha' WHERE Tema = 'CuandoCosechar';
                UPDATE ContenidosChatbot SET Tema = 'AbonadoNutricion' WHERE Tema = 'Abonado';
                UPDATE ContenidosChatbot SET Tema = 'PlagasEnfermedades' WHERE Tema = 'PlagasEnfermedades';
                UPDATE ContenidosChatbot SET Tema = 'PodasManejo' WHERE Tema IN ('PodaMantenimiento', 'ManejoAgroecologico');
                UPDATE ContenidosChatbot SET Tema = 'Riego' WHERE Tema = 'RiegoDrenaje';
                UPDATE ContenidosChatbot SET Tema = 'PostcosechaAlmacenamiento' WHERE Tema = 'PostcosechaManejo';
                UPDATE ContenidosChatbot SET Tema = 'Comercializacion' WHERE Tema = 'ComercializacionCalidad';
            ");
            logger.LogInformation("Temas de ContenidosChatbot sincronizados con los 8 temas oficiales.");
        }
        catch (Exception ex)
        {
            logger.LogWarning("Advertencia al sincronizar temas de ContenidosChatbot: {Msg}", ex.Message);
        }

        // Asegurar que la columna Descripcion exista en la tabla Familias
        try
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Familias ADD COLUMN IF NOT EXISTS Descripcion TEXT NULL;");
        }
        catch
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Familias ADD COLUMN Descripcion TEXT NULL;");
            }
            catch { }
        }

        // Asegurar la existencia de las tablas SIPSA DANE
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS BoletinesSipsaInfo (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Periodo VARCHAR(150) NOT NULL,
                    BoletinTecnicoUrl VARCHAR(500) NULL,
                    BoletinTecnicoTitulo VARCHAR(300) NULL,
                    AnexoExcelUrl VARCHAR(500) NULL,
                    AnexoExcelTitulo VARCHAR(300) NULL,
                    FechaPublicacion DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
                    FechaSincronizacion DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
                    TotalRegistros INT NOT NULL DEFAULT 0,
                    EsExitoso TINYINT(1) NOT NULL DEFAULT 1
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                CREATE TABLE IF NOT EXISTS PreciosMayoristasSipsa (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    GrupoAlimento VARCHAR(100) NOT NULL,
                    Producto VARCHAR(150) NOT NULL,
                    MercadoMayorista VARCHAR(150) NOT NULL,
                    PrecioMinimo DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                    PrecioMaximo DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                    PrecioMedio DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                    Tendencia VARCHAR(10) NOT NULL DEFAULT '=',
                    PeriodoBoletin VARCHAR(150) NOT NULL,
                    FechaBoletin DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
                    FechaActualizacion DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
                    CultivoId INT NULL,
                    INDEX IX_PreciosMayoristasSipsa_Prod_Merc (Producto, MercadoMayorista),
                    INDEX IX_PreciosMayoristasSipsa_Grupo (GrupoAlimento),
                    INDEX IX_PreciosMayoristasSipsa_Fecha (FechaBoletin)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");
            logger.LogInformation("Tablas de cotizaciones mayoristas SIPSA DANE verificadas correctamente.");
        }
        catch (Exception ex)
        {
            logger.LogWarning("No se pudo auto-crear tablas SIPSA: {Msg}", ex.Message);
        }

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
    var contrasena = config.GetValue<string>("Admin:ContrasenaPorDefecto", "InnovaFemIA2026!")!;
    var email = config.GetValue<string>("Admin:EmailPorDefecto", "dayana.hurtado@estudiante.fup.edu.co")!;

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
