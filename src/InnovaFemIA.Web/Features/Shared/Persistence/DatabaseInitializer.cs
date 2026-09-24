using InnovaFemIA.Web.Features.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Shared.Persistence;

/// <summary>
/// Servicio de inicialización resiliente de la base de datos para entornos de producción y contenedores Docker.
/// Aplica migraciones de Entity Framework Core de forma idempotente y aprovisiona exclusivamente al usuario administrador inicial.
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
            throw new InvalidOperationException("No se pudo conectar a la base de datos MySQL tras agotar los reintentos. Verifique la conectividad de la base de datos.");
        }

        try
        {
            // Configurar max_allowed_packet a 512 MB si el servidor lo permite
            try
            {
                await db.Database.ExecuteSqlRawAsync("SET GLOBAL max_allowed_packet = 536870912;");
                await db.Database.CloseConnectionAsync();
                logger.LogInformation("MySQL max_allowed_packet verificado/configurado a 512 MB.");
            }
            catch (Exception ex)
            {
                logger.LogDebug("Aviso informativo: SET GLOBAL max_allowed_packet delegado a la configuración del servidor: {Msg}", ex.Message);
            }

            // Compatibilidad de esquema y sincronización de catálogos
            await EnsureSchemaCompatibilityAsync(db, logger);

            // Aprovisionar exclusivamente al Administrador por defecto (sin datos de prueba)
            await SeedAdminAsync(db, config, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante la inicialización de esquemas o seed administrativo.");
        }
    }

    /// <summary>
    /// Asegura la existencia de tablas de soporte e índices requeridos si la BD proviene de versiones anteriores.
    /// </summary>
    private static async Task EnsureSchemaCompatibilityAsync(AppDbContext db, ILogger logger)
    {
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
            // Asegurar columna Descripcion en Familias si es una base de datos preexistente
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
        }
        catch { }

        try
        {
            // Asegurar existencia de tablas del DANE SIPSA
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
        }
        catch (Exception ex)
        {
            logger.LogDebug("Aviso informativo tablas SIPSA: {Msg}", ex.Message);
        }
    }

    /// <summary>
    /// Aprovisiona al usuario administrador por defecto del sistema si no existe en la base de datos.
    /// </summary>
    private static async Task SeedAdminAsync(AppDbContext db, IConfiguration config, ILogger logger)
    {
        var usuario = config.GetValue<string>("Admin:UsuarioPorDefecto", "admin")!;
        var contrasena = config.GetValue<string>("Admin:ContrasenaPorDefecto", "InnovaFemIA2026!")!;
        var email = config.GetValue<string>("Admin:EmailPorDefecto", "dayana.hurtado@estudiante.fup.edu.co")!;

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
