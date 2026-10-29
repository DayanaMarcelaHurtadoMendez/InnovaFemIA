using InnovaFemIA.Web.Features.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Shared.Persistence;

/// <summary>
/// Servicio de inicialización resiliente de la base de datos para el Módulo de Chatbot.
/// Aplica migraciones de Entity Framework Core de forma idempotente y aprovisiona al administrador inicial.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AppDbContext>();
        var config = app.Configuration;
        var logger = services.GetRequiredService<ILogger<AppDbContext>>();

        const int maxIntentos = 10;
        var espera = TimeSpan.FromSeconds(3);
        bool dbConectada = false;

        for (int intento = 1; intento <= maxIntentos; intento++)
        {
            try
            {
                logger.LogInformation("Verificando conexión con MySQL y aplicando migraciones EF Core (Intento {Intento}/{Max})...", intento, maxIntentos);
                await db.Database.MigrateAsync();
                logger.LogInformation("Migraciones de base de datos aplicadas correctamente.");
                dbConectada = true;
                break;
            }
            catch (Exception ex) when (intento < maxIntentos)
            {
                logger.LogWarning("Base de datos no disponible aún ({Intento}/{Max}): {Msg}. Reintentando en {Segundos}s...",
                    intento, maxIntentos, ex.Message, espera.TotalSeconds);
                await Task.Delay(espera);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error crítico: No se pudo conectar a la base de datos tras {Max} intentos.", maxIntentos);
            }
        }

        if (!dbConectada)
        {
            throw new InvalidOperationException("No se pudo conectar a la base de datos MySQL tras agotar los reintentos.");
        }

        try
        {
            // Sincronizar nombres de temas oficiales del chatbot si hay registros previos
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
        }
        catch { }

        try
        {
            // Aprovisionar al Administrador por defecto
            await SeedAdminAsync(db, config, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante la inicialización administrativa.");
        }
    }

    private static async Task SeedAdminAsync(AppDbContext db, IConfiguration config, ILogger logger)
    {
        var usuario = config.GetValue<string>("Admin:UsuarioPorDefecto", "admin")!;
        var contrasena = config.GetValue<string>("Admin:ContrasenaPorDefecto", "CambiarEstaContrasena123!")!;
        var email = config.GetValue<string>("Admin:EmailPorDefecto", "admin@innovafemia.local")!;

        var admin = await db.Administradores.FirstOrDefaultAsync(a => a.NombreUsuario == usuario);
        if (admin == null)
        {
            db.Administradores.Add(new Administrador
            {
                NombreUsuario = usuario,
                HashContrasena = BCrypt.Net.BCrypt.HashPassword(contrasena, workFactor: 12),
                NombreCompleto = "Administrador InnovaFemIA",
                Email = email,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            logger.LogInformation("Usuario administrador inicial aprovisionado: {Usuario}", usuario);
        }
        else if (admin.Email != email)
        {
            admin.Email = email;
            await db.SaveChangesAsync();
            logger.LogInformation("Email de administrador actualizado a: {Email}", email);
        }
    }
}
