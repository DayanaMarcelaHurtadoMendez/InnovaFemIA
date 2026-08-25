using InnovaFemIA.Web.Features.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Shared.Persistence;

/// <summary>
/// DbContext principal de InnovaFemIA.
/// Configura todas las entidades de los 3 módulos con Fluent API.
/// Base de datos: MySQL (MariaDB) via Pomelo EF Core provider.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ─── Módulo compartido / Mapa ──────────────────────────────────────────
    public DbSet<Familia> Familias => Set<Familia>();
    public DbSet<Cultivo> Cultivos => Set<Cultivo>();
    public DbSet<FamiliaCultivo> FamiliaCultivos => Set<FamiliaCultivo>();
    public DbSet<ActorMapa> ActoresMapa => Set<ActorMapa>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<PrecioMercado> PreciosMercado => Set<PrecioMercado>();
    public DbSet<PrecioMayoristaSipsa> PreciosMayoristasSipsa => Set<PrecioMayoristaSipsa>();
    public DbSet<BoletinSipsaInfo> BoletinesSipsaInfo => Set<BoletinSipsaInfo>();
    public DbSet<ReglasLunares> ReglasLunares => Set<ReglasLunares>();

    // ─── Módulo 3 — Biocomercio ────────────────────────────────────────────
    public DbSet<CultivoTiempo> CultivosTiempos => Set<CultivoTiempo>();
    public DbSet<UsuarioFamilia> UsuariosFamilias => Set<UsuarioFamilia>();

    // ─── Módulo 2 — Chatbot ────────────────────────────────────────────────
    public DbSet<ContenidoChatbot> ContenidosChatbot => Set<ContenidoChatbot>();
    public DbSet<LeccionChatbot> LeccionesChatbot => Set<LeccionChatbot>();
    public DbSet<PreguntaLeccion> PreguntasLecciones => Set<PreguntaLeccion>();
    public DbSet<OpcionPregunta> OpcionesPregunta => Set<OpcionPregunta>();

    // ─── Sistema ───────────────────────────────────────────────────────────
    public DbSet<Administrador> Administradores => Set<Administrador>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<RegistroSistema> RegistrosSistema => Set<RegistroSistema>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Familia ───────────────────────────────────────────────────────
        modelBuilder.Entity<Familia>(e =>
        {
            e.ToTable("Familias");
            e.HasKey(f => f.Id);
            e.Property(f => f.Nombre).IsRequired().HasMaxLength(200);
            e.Property(f => f.Vereda).IsRequired().HasMaxLength(100);
            e.Property(f => f.Municipio).IsRequired().HasMaxLength(100).HasDefaultValue("Caldono");
            e.Property(f => f.NombreContacto).IsRequired().HasMaxLength(200);
            e.Property(f => f.TelefonoContacto).IsRequired().HasMaxLength(20);
            e.Property(f => f.Descripcion).HasMaxLength(1000);
            e.Property(f => f.FotoBytes).HasColumnType("LONGBLOB");
            e.Property(f => f.FotoMimeType).HasMaxLength(50);
            e.Property(f => f.Activo).HasDefaultValue(true);
            e.Property(f => f.FechaCreacion).HasDefaultValueSql("UTC_TIMESTAMP(6)");
        });

        // ── Cultivo ───────────────────────────────────────────────────────
        modelBuilder.Entity<Cultivo>(e =>
        {
            e.ToTable("Cultivos");
            e.HasKey(c => c.Id);
            e.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
            e.Property(c => c.Descripcion).HasMaxLength(500);
            e.Property(c => c.Icono).HasMaxLength(20).HasDefaultValue("🌱");
            e.Property(c => c.Activo).HasDefaultValue(true);

            // Seed data inicial
            e.HasData(
                new Cultivo { Id = 1, Nombre = "Café", Descripcion = "Cultivo de café tradicional", Icono = "☕" },
                new Cultivo { Id = 2, Nombre = "Plátano", Descripcion = "Cultivo de plátano", Icono = "🍌" },
                new Cultivo { Id = 3, Nombre = "Aguacate", Descripcion = "Cultivo de aguacate Hass", Icono = "🥑" },
                new Cultivo { Id = 4, Nombre = "Yuca", Descripcion = "Cultivo de yuca", Icono = "🌿" },
                new Cultivo { Id = 5, Nombre = "Caña", Descripcion = "Caña de azúcar", Icono = "🎋" },
                new Cultivo { Id = 6, Nombre = "Maíz", Descripcion = "Cultivo de maíz", Icono = "🌽" },
                new Cultivo { Id = 7, Nombre = "Limón", Descripcion = "Cultivo de limón", Icono = "🍋" }
            );
        });

        // ── FamiliaCultivo (pivot N:M) ────────────────────────────────────
        modelBuilder.Entity<FamiliaCultivo>(e =>
        {
            e.ToTable("FamiliaCultivos");
            e.HasKey(fc => new { fc.FamiliaId, fc.CultivoId });

            e.HasOne(fc => fc.Familia)
                .WithMany(f => f.FamiliaCultivos)
                .HasForeignKey(fc => fc.FamiliaId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(fc => fc.Cultivo)
                .WithMany(c => c.FamiliaCultivos)
                .HasForeignKey(fc => fc.CultivoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ActorMapa ─────────────────────────────────────────────────────
        modelBuilder.Entity<ActorMapa>(e =>
        {
            e.ToTable("ActoresMapa");
            e.HasKey(a => a.Id);
            e.Property(a => a.Nombre).IsRequired().HasMaxLength(200);
            e.Property(a => a.TipoActor).IsRequired().HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.Descripcion).HasMaxLength(500);
            e.Property(a => a.DatosContacto).HasMaxLength(300);
            e.Property(a => a.LogoBytes).HasColumnType("LONGBLOB");
            e.Property(a => a.LogoMimeType).HasMaxLength(50);
            e.Property(a => a.Activo).HasDefaultValue(true);
            e.Property(a => a.FechaCreacion).HasDefaultValueSql("UTC_TIMESTAMP(6)");
        });

        // ── Stock ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Stock>(e =>
        {
            e.ToTable("Stocks");
            e.HasKey(s => s.Id);
            // Constraint: una familia no puede tener dos registros para el mismo cultivo
            e.HasIndex(s => new { s.FamiliaId, s.CultivoId }).IsUnique();

            e.HasOne(s => s.Familia)
                .WithMany(f => f.Stocks)
                .HasForeignKey(s => s.FamiliaId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.Cultivo)
                .WithMany(c => c.Stocks)
                .HasForeignKey(s => s.CultivoId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Property(s => s.Notas).HasMaxLength(500);
            e.Property(s => s.UltimaActualizacion).HasDefaultValueSql("UTC_TIMESTAMP(6)");
        });

        // ── PrecioMercado ─────────────────────────────────────────────────
        modelBuilder.Entity<PrecioMercado>(e =>
        {
            e.ToTable("PreciosMercado");
            e.HasKey(p => p.Id);
            e.Property(p => p.Precio).HasColumnType("decimal(18,2)");
            e.Property(p => p.PrecioAnterior).HasColumnType("decimal(18,2)");
            e.Property(p => p.Fuente).HasMaxLength(100).HasDefaultValue("DANE");
            e.Property(p => p.FechaActualizacion).HasDefaultValueSql("UTC_TIMESTAMP(6)");
            e.HasIndex(p => new { p.CultivoId, p.FechaActualizacion });

            e.HasOne(p => p.Cultivo)
                .WithMany(c => c.PreciosMercado)
                .HasForeignKey(p => p.CultivoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PrecioMayoristaSipsa (DANE SIPSA) ─────────────────────────────
        modelBuilder.Entity<PrecioMayoristaSipsa>(e =>
        {
            e.ToTable("PreciosMayoristasSipsa");
            e.HasKey(p => p.Id);
            e.Property(p => p.GrupoAlimento).IsRequired().HasMaxLength(100);
            e.Property(p => p.Producto).IsRequired().HasMaxLength(150);
            e.Property(p => p.MercadoMayorista).IsRequired().HasMaxLength(150);
            e.Property(p => p.PrecioMinimo).HasColumnType("decimal(18,2)");
            e.Property(p => p.PrecioMaximo).HasColumnType("decimal(18,2)");
            e.Property(p => p.PrecioMedio).HasColumnType("decimal(18,2)");
            e.Property(p => p.Tendencia).HasMaxLength(10).HasDefaultValue("=");
            e.Property(p => p.PeriodoBoletin).HasMaxLength(150);
            e.Property(p => p.FechaBoletin).HasDefaultValueSql("UTC_TIMESTAMP(6)");
            e.Property(p => p.FechaActualizacion).HasDefaultValueSql("UTC_TIMESTAMP(6)");

            e.HasIndex(p => new { p.Producto, p.MercadoMayorista });
            e.HasIndex(p => p.GrupoAlimento);
            e.HasIndex(p => p.FechaBoletin);

            e.HasOne(p => p.Cultivo)
                .WithMany()
                .HasForeignKey(p => p.CultivoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── BoletinSipsaInfo (Metadatos del Boletín DANE) ───────────────────
        modelBuilder.Entity<BoletinSipsaInfo>(e =>
        {
            e.ToTable("BoletinesSipsaInfo");
            e.HasKey(b => b.Id);
            e.Property(b => b.Periodo).IsRequired().HasMaxLength(150);
            e.Property(b => b.BoletinTecnicoUrl).HasMaxLength(500);
            e.Property(b => b.BoletinTecnicoTitulo).HasMaxLength(300);
            e.Property(b => b.AnexoExcelUrl).HasMaxLength(500);
            e.Property(b => b.AnexoExcelTitulo).HasMaxLength(300);
            e.Property(b => b.FechaPublicacion).HasDefaultValueSql("UTC_TIMESTAMP(6)");
            e.Property(b => b.FechaSincronizacion).HasDefaultValueSql("UTC_TIMESTAMP(6)");
            e.Property(b => b.EsExitoso).HasDefaultValue(true);
        });

        // ── ReglasLunares ─────────────────────────────────────────────────
        modelBuilder.Entity<ReglasLunares>(e =>
        {
            e.ToTable("ReglasLunares");
            e.HasKey(r => r.Id);
            e.Property(r => r.FaseLunar).IsRequired().HasConversion<string>().HasMaxLength(30);
            e.Property(r => r.Practica).IsRequired().HasMaxLength(100);
            e.Property(r => r.Recomendacion).IsRequired().HasMaxLength(1000);
            e.Property(r => r.NivelConfianza).HasMaxLength(20).HasDefaultValue("Alta");

            e.HasOne(r => r.Cultivo)
                .WithMany(c => c.ReglasLunares)
                .HasForeignKey(r => r.CultivoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── CultivoTiempo ─────────────────────────────────────────────────
        modelBuilder.Entity<CultivoTiempo>(e =>
        {
            e.ToTable("CultivosTiempos");
            e.HasKey(ct => ct.Id);
            e.Property(ct => ct.Actividad).IsRequired().HasConversion<string>().HasMaxLength(50);
            e.Property(ct => ct.DescripcionTemplate).HasMaxLength(500);
            e.Property(ct => ct.Notas).HasMaxLength(500);

            e.HasOne(ct => ct.Cultivo)
                .WithMany(c => c.CultivosTiempos)
                .HasForeignKey(ct => ct.CultivoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed: tiempos estándar de cosecha por cultivo y actividad
            e.HasData(
                new CultivoTiempo { Id = 1, CultivoId = 1, Actividad = ActividadAgricola.Siembra, MesesHastaCosecha = 8, DescripcionTemplate = "Tu café sembrado el {fecha} estaría listo para cosechar aproximadamente en {mes} de {año}" },
                new CultivoTiempo { Id = 2, CultivoId = 2, Actividad = ActividadAgricola.Siembra, MesesHastaCosecha = 9, DescripcionTemplate = "Tu plátano sembrado el {fecha} estaría listo para cosechar aproximadamente en {mes} de {año}" },
                new CultivoTiempo { Id = 3, CultivoId = 3, Actividad = ActividadAgricola.Siembra, MesesHastaCosecha = 14, DescripcionTemplate = "Tu aguacate sembrado el {fecha} estaría listo para cosechar aproximadamente en {mes} de {año}" },
                new CultivoTiempo { Id = 4, CultivoId = 4, Actividad = ActividadAgricola.Siembra, MesesHastaCosecha = 6, DescripcionTemplate = "Tu yuca sembrada el {fecha} estaría lista para cosechar aproximadamente en {mes} de {año}" },
                new CultivoTiempo { Id = 5, CultivoId = 5, Actividad = ActividadAgricola.Siembra, MesesHastaCosecha = 12, DescripcionTemplate = "Tu caña sembrada el {fecha} estaría lista para cosechar aproximadamente en {mes} de {año}" },
                new CultivoTiempo { Id = 6, CultivoId = 6, Actividad = ActividadAgricola.Siembra, MesesHastaCosecha = 4, DescripcionTemplate = "Tu maíz sembrado el {fecha} estaría listo para cosechar aproximadamente en {mes} de {año}" },
                new CultivoTiempo { Id = 7, CultivoId = 7, Actividad = ActividadAgricola.Siembra, MesesHastaCosecha = 5, DescripcionTemplate = "Tu limón sembrado el {fecha} estaría listo para cosechar aproximadamente en {mes} de {año}" }
            );
        });

        // ── UsuarioFamilia ─────────────────────────────────────────────────
        modelBuilder.Entity<UsuarioFamilia>(e =>
        {
            e.ToTable("UsuariosFamilias");
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.NombreUsuario).IsUnique();
            e.Property(u => u.NombreUsuario).IsRequired().HasMaxLength(100);
            e.Property(u => u.HashContrasena).IsRequired().HasMaxLength(256);
            e.Property(u => u.Activo).HasDefaultValue(true);
            e.Property(u => u.FechaCreacion).HasDefaultValueSql("UTC_TIMESTAMP(6)");

            e.HasOne(u => u.Familia)
                .WithOne(f => f.UsuarioFamilia)
                .HasForeignKey<UsuarioFamilia>(u => u.FamiliaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Administrador ─────────────────────────────────────────────────
        modelBuilder.Entity<Administrador>(e =>
        {
            e.ToTable("Administradores");
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.NombreUsuario).IsUnique();
            e.Property(a => a.NombreUsuario).IsRequired().HasMaxLength(100);
            e.Property(a => a.HashContrasena).IsRequired().HasMaxLength(256);
            e.Property(a => a.NombreCompleto).HasMaxLength(200);
            e.Property(a => a.Email).HasMaxLength(200);
            e.Property(a => a.Activo).HasDefaultValue(true);
            e.Property(a => a.FechaCreacion).HasDefaultValueSql("UTC_TIMESTAMP(6)");
        });

        // ── ApiKey ────────────────────────────────────────────────────────
        modelBuilder.Entity<ApiKey>(e =>
        {
            e.ToTable("ApiKeys");
            e.HasKey(k => k.Id);
            e.HasIndex(k => k.Clave).IsUnique();
            e.Property(k => k.Clave).IsRequired().HasMaxLength(256);
            e.Property(k => k.NombreApp).IsRequired().HasMaxLength(100);
            e.Property(k => k.Descripcion).HasMaxLength(500);
            e.Property(k => k.Activo).HasDefaultValue(true);
            e.Property(k => k.FechaCreacion).HasDefaultValueSql("UTC_TIMESTAMP(6)");
            e.Property(k => k.UltimaIpUso).HasMaxLength(50);
        });

        // ── RegistroSistema ───────────────────────────────────────────────
        modelBuilder.Entity<RegistroSistema>(e =>
        {
            e.ToTable("RegistrosSistema");
            e.HasKey(r => r.Id);
            e.Property(r => r.TipoEvento).IsRequired().HasMaxLength(50);
            e.Property(r => r.Descripcion).IsRequired().HasColumnType("TEXT");
            e.Property(r => r.Modulo).IsRequired().HasMaxLength(50);
            e.Property(r => r.StackTrace).HasColumnType("TEXT");
            e.Property(r => r.IpOrigen).HasMaxLength(50);
            e.Property(r => r.FechaHoraUtc).HasDefaultValueSql("UTC_TIMESTAMP(6)");
            e.HasIndex(r => new { r.Modulo, r.FechaHoraUtc });
        });

        // ── ContenidoChatbot ──────────────────────────────────────────────
        modelBuilder.Entity<ContenidoChatbot>(e =>
        {
            e.ToTable("ContenidosChatbot");
            e.HasKey(c => c.Id);
            e.Property(c => c.Tema).IsRequired().HasConversion<string>().HasMaxLength(50);
            e.Property(c => c.TipoRecurso).IsRequired().HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.Titulo).IsRequired().HasMaxLength(200);
            e.Property(c => c.Url).HasMaxLength(500);
            e.Property(c => c.ArchivoBytes).HasColumnType("LONGBLOB");
            e.Property(c => c.ArchivoMimeType).HasMaxLength(50);
            e.Property(c => c.ArchivoNombre).HasMaxLength(200);
            e.Property(c => c.FechaCreacion).HasDefaultValueSql("UTC_TIMESTAMP(6)");
            e.HasIndex(c => new { c.CultivoId, c.Tema, c.Orden });

            e.HasOne(c => c.Cultivo)
                .WithMany(cu => cu.ContenidosChatbot)
                .HasForeignKey(c => c.CultivoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── LeccionChatbot ────────────────────────────────────────────────
        modelBuilder.Entity<LeccionChatbot>(e =>
        {
            e.ToTable("LeccionesChatbot");
            e.HasKey(l => l.Id);
            e.HasIndex(l => l.NumeroLeccion).IsUnique();
            e.Property(l => l.Titulo).IsRequired().HasMaxLength(200);
            e.Property(l => l.Descripcion).HasMaxLength(500);
            e.Property(l => l.ImagenBytes).HasColumnType("LONGBLOB");
            e.Property(l => l.ImagenMimeType).HasMaxLength(50);
            e.Property(l => l.FechaCreacion).HasDefaultValueSql("UTC_TIMESTAMP(6)");
        });

        // ── PreguntaLeccion ───────────────────────────────────────────────
        modelBuilder.Entity<PreguntaLeccion>(e =>
        {
            e.ToTable("PreguntasLecciones");
            e.HasKey(p => p.Id);
            e.Property(p => p.TextoPregunta).IsRequired().HasMaxLength(500);
            e.Property(p => p.ContenidoExplicativo).HasColumnType("TEXT");
            e.Property(p => p.ImagenExplicativaBytes).HasColumnType("LONGBLOB");
            e.Property(p => p.ImagenExplicativaMimeType).HasMaxLength(50);

            e.HasOne(p => p.Leccion)
                .WithMany(l => l.Preguntas)
                .HasForeignKey(p => p.LeccionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── OpcionPregunta ─────────────────────────────────────────────────
        modelBuilder.Entity<OpcionPregunta>(e =>
        {
            e.ToTable("OpcionesPregunta");
            e.HasKey(o => o.Id);
            e.Property(o => o.TextoOpcion).IsRequired().HasMaxLength(300);
            e.Property(o => o.Retroalimentacion).HasMaxLength(500);

            e.HasOne(o => o.Pregunta)
                .WithMany(p => p.Opciones)
                .HasForeignKey(o => o.PreguntaId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
