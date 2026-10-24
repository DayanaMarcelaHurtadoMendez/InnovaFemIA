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
builder.Services.AddScoped<InnovaFemIA.Web.Features.Biocomercio.DeteccionEnfermedades.IRoboflowEnfermedadesService, InnovaFemIA.Web.Features.Biocomercio.DeteccionEnfermedades.RoboflowEnfermedadesService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// ─── SESIÓN (cookies para Admin y Familias) ───────────────────────────────────
var sesionExpiracion = builder.Configuration.GetValue<int>("Sesion:TiempoExpiracionMinutos", 60);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(sesionExpiracion);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Path = "/";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ─── PROTECCIÓN CONTRA CSRF / ANTIFORGERY COMPATIBLE CON PROXY REVERSO ─────
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "InnovaFemIA.Antiforgery";
    options.Cookie.Path = "/";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.HeaderName = "X-XSRF-TOKEN";
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
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.Lax;
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
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.Lax;
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
// Soporte para Proxy Reverso (Nginx, Traefik, HAProxy, IIS)
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Soporte para sub-rutas / PathBase configurable via variable de entorno PATH_BASE o X-Forwarded-Prefix
// Permite desplegar bajo https://i2thub.icesi.edu.co:5443/innovafemia o en la raíz /
var configuredPathBase = Environment.GetEnvironmentVariable("PATH_BASE")
                      ?? builder.Configuration["PATH_BASE"]
                      ?? builder.Configuration["App:PathBase"];

if (!string.IsNullOrWhiteSpace(configuredPathBase))
{
    configuredPathBase = "/" + configuredPathBase.Trim().Trim('/');
    if (configuredPathBase == "/" || configuredPathBase.Equals("/ROOT", StringComparison.OrdinalIgnoreCase) || configuredPathBase.Equals("/NONE", StringComparison.OrdinalIgnoreCase))
    {
        configuredPathBase = null;
    }
}
else
{
    // Predeterminado para producción en el servidor de la Universidad ICESI
    configuredPathBase = "/innovafemia";
}

app.Use(async (context, next) =>
{
    // 1. Sincronizar Scheme (https) cuando el proxy inverso termina SSL
    if (context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var protoHeader) && !string.IsNullOrEmpty(protoHeader))
    {
        var proto = protoHeader.ToString().Split(',')[0].Trim();
        if (!string.IsNullOrEmpty(proto))
        {
            context.Request.Scheme = proto;
        }
    }
    else if (context.Request.Headers.TryGetValue("Referer", out var refProtoHeader) && refProtoHeader.ToString().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
        context.Request.Scheme = "https";
    }
    else if (context.Request.Headers.TryGetValue("Origin", out var orgProtoHeader) && orgProtoHeader.ToString().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
        context.Request.Scheme = "https";
    }

    // 2. Sincronizar Host y Puerto público (ej: i2thub.icesi.edu.co:5443)
    // Esto asegura que context.Request.Host coincida con el Origin / Referer público del navegador
    // evitando rechazos HTTP 400 por desajuste de origen en formularios con antiforgery detrás del proxy
    if (context.Request.Headers.TryGetValue("Origin", out var originHeader) && Uri.TryCreate(originHeader.ToString(), UriKind.Absolute, out var originUri))
    {
        context.Request.Host = new HostString(originUri.Authority);
    }
    else if (context.Request.Headers.TryGetValue("X-Forwarded-Host", out var fwdHost) && !string.IsNullOrEmpty(fwdHost))
    {
        var hostStr = fwdHost.ToString().Split(',')[0].Trim();
        if (context.Request.Headers.TryGetValue("X-Forwarded-Port", out var fwdPort) && !string.IsNullOrEmpty(fwdPort) && !hostStr.Contains(':'))
        {
            var portStr = fwdPort.ToString().Split(',')[0].Trim();
            if (portStr != "80" && portStr != "443")
            {
                hostStr = $"{hostStr}:{portStr}";
            }
        }
        context.Request.Host = new HostString(hostStr);
    }
    else if (context.Request.Headers.TryGetValue("Referer", out var refererHeader) && Uri.TryCreate(refererHeader.ToString(), UriKind.Absolute, out var refUri))
    {
        context.Request.Host = new HostString(refUri.Authority);
    }

    // 3. Resolver PathBase (/innovafemia)
    string? prefix = null;

    // Detectar prefijo dinámico desde header X-Forwarded-Prefix (si el proxy lo reenvía)
    if (context.Request.Headers.TryGetValue("X-Forwarded-Prefix", out var prefixHeader))
    {
        var rawPrefix = prefixHeader.ToString().Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(rawPrefix))
        {
            prefix = rawPrefix.StartsWith('/') ? rawPrefix : "/" + rawPrefix;
        }
    }

    // Detectar si el Referer viene con /innovafemia (petición originada en navegador bajo el proxy)
    if (string.IsNullOrEmpty(prefix) && context.Request.Headers.TryGetValue("Referer", out var refererH))
    {
        var referer = refererH.ToString();
        if (referer.Contains("/innovafemia", StringComparison.OrdinalIgnoreCase))
        {
            prefix = "/innovafemia";
        }
    }

    // Detectar si la URL entrante contiene /innovafemia
    if (string.IsNullOrEmpty(prefix) && context.Request.Path.Value?.Contains("/innovafemia", StringComparison.OrdinalIgnoreCase) == true)
    {
        prefix = "/innovafemia";
    }

    // Si no se detectó dinámicamente, usar la configuración fija (default /innovafemia)
    if (string.IsNullOrEmpty(prefix) && !string.IsNullOrWhiteSpace(configuredPathBase))
    {
        prefix = configuredPathBase;
    }

    if (!string.IsNullOrEmpty(prefix))
    {
        // Caso A: La petición entrante aún contiene el prefijo en Path (el proxy NO lo removió)
        if (context.Request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase, out var remaining))
        {
            context.Request.PathBase = context.Request.PathBase.Add(prefix);
            context.Request.Path = remaining;
        }
        // Caso B: El proxy ya removió el prefijo antes de llegar a Kestrel.
        // Asignamos PathBase para que los helpers de Razor (~/...), cookies y redirecciones usen el prefijo público.
        else if (string.IsNullOrEmpty(context.Request.PathBase.Value))
        {
            context.Request.PathBase = new PathString(prefix);
        }
    }

    await next();
});

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
