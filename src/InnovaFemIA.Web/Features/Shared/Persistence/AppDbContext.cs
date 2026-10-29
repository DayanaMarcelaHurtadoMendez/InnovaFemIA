using InnovaFemIA.Web.Features.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Shared.Persistence;

/// <summary>
/// DbContext del Módulo de Chatbot y Asistente Educativo Asalia.
/// Base de datos: MySQL (MariaDB) via Pomelo EF Core provider.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ─── Módulo 2 — Chatbot y Educación Agroecológica ──────────────────────
    public DbSet<Cultivo> Cultivos => Set<Cultivo>();
    public DbSet<ContenidoChatbot> ContenidosChatbot => Set<ContenidoChatbot>();
    public DbSet<LeccionChatbot> LeccionesChatbot => Set<LeccionChatbot>();
    public DbSet<PreguntaLeccion> PreguntasLecciones => Set<PreguntaLeccion>();
    public DbSet<OpcionPregunta> OpcionesPregunta => Set<OpcionPregunta>();

    // ─── Sistema y Administración ──────────────────────────────────────────
    public DbSet<Administrador> Administradores => Set<Administrador>();
    public DbSet<RegistroSistema> RegistrosSistema => Set<RegistroSistema>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
            e.Property(c => c.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
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
            e.Property(l => l.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
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
            e.Property(a => a.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
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
            e.Property(r => r.FechaHoraUtc).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            e.HasIndex(r => new { r.Modulo, r.FechaHoraUtc });
        });
    }
}
