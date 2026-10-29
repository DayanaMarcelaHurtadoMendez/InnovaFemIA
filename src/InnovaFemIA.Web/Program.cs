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
            mysqlOptions.CommandTimeout(300); // 5 minutos para subida de videos/documentos pedagógicos
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
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddSingleton<InnovaFemIA.Web.Features.Shared.Services.IMediaCacheService, InnovaFemIA.Web.Features.Shared.Services.MediaCacheService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// ─── SESIÓN (cookies para Admin) ─────────────────────────────────────────────
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

// ─── PROTECCIÓN CONTRA CSRF / ANTIFORGERY ─────────────────────────────────────
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "InnovaFemIA.Antiforgery";
    options.Cookie.Path = "/";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.HeaderName = "X-XSRF-TOKEN";
});

// ─── AUTENTICACIÓN CON COOKIES (ADMIN) ────────────────────────────────────────
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
});

builder.Services.AddAuthorization();
builder.Services.AddHttpClient();

// ─── LOGGING ──────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ────────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ────────────────────────────────────────────────────────────────────────────

// ─── PIPELINE HTTP ────────────────────────────────────────────────────────────
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

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
    configuredPathBase = "/innovafemia";
}

app.Use(async (context, next) =>
{
    if (context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var protoHeader) && !string.IsNullOrEmpty(protoHeader))
    {
        var proto = protoHeader.ToString().Split(',')[0].Trim();
        if (!string.IsNullOrEmpty(proto))
        {
            context.Request.Scheme = proto;
        }
    }

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

    string? prefix = null;
    if (context.Request.Headers.TryGetValue("X-Forwarded-Prefix", out var prefixHeader))
    {
        var rawPrefix = prefixHeader.ToString().Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(rawPrefix))
        {
            prefix = rawPrefix.StartsWith('/') ? rawPrefix : "/" + rawPrefix;
        }
    }

    if (string.IsNullOrEmpty(prefix) && context.Request.Headers.TryGetValue("Referer", out var refererH))
    {
        var referer = refererH.ToString();
        if (referer.Contains("/innovafemia", StringComparison.OrdinalIgnoreCase))
        {
            prefix = "/innovafemia";
        }
    }

    if (string.IsNullOrEmpty(prefix) && context.Request.Path.Value?.Contains("/innovafemia", StringComparison.OrdinalIgnoreCase) == true)
    {
        prefix = "/innovafemia";
    }

    if (string.IsNullOrEmpty(prefix) && !string.IsNullOrWhiteSpace(configuredPathBase))
    {
        prefix = configuredPathBase;
    }

    if (!string.IsNullOrEmpty(prefix))
    {
        if (context.Request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase, out var remaining))
        {
            context.Request.PathBase = context.Request.PathBase.Add(prefix);
            context.Request.Path = remaining;
        }
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

var defaultCulture = new System.Globalization.CultureInfo("es-CO");
defaultCulture.NumberFormat.NumberDecimalSeparator = ".";
defaultCulture.NumberFormat.NumberGroupSeparator = ",";
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture),
    SupportedCultures = [defaultCulture, System.Globalization.CultureInfo.InvariantCulture],
    SupportedUICultures = [defaultCulture, System.Globalization.CultureInfo.InvariantCulture]
});

app.UseCaseInsensitiveStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", app = "InnovaFemIA - Chatbot", timestamp = DateTime.UtcNow }));

await app.InitializeDatabaseAsync();

app.Run();
