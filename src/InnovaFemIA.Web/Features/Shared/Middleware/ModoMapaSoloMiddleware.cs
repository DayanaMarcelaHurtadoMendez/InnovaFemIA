namespace InnovaFemIA.Web.Features.Shared.Middleware;

/// <summary>
/// Middleware para restringir la navegación pública en el contenedor de "Modo Mapa",
/// redirigiendo intentos de acceso a chatbot o biocomercio al directorio interactivo.
/// </summary>
public static class ModoMapaSoloMiddlewareExtensions
{
    public static IApplicationBuilder UseModoMapaSolo(this WebApplication app)
    {
        var modoMapaSolo = app.Configuration.GetValue<bool>("ModoMapaSolo", false);
        if (!modoMapaSolo)
        {
            return app;
        }

        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if ((path.StartsWith("/chatbot", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("/biocomercio", StringComparison.OrdinalIgnoreCase)) &&
                !(context.User.Identity?.IsAuthenticated == true && (context.User.IsInRole("Administrador") || context.User.IsInRole("Admin"))))
            {
                context.Response.Redirect("/mapa/directorio");
                return;
            }
            await next();
        });

        return app;
    }
}
