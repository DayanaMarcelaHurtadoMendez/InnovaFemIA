using System.Globalization;
using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Entities;
using InnovaFemIA.Web.Features.Shared.Persistence;
using InnovaFemIA.Web.Features.Shared.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Mapa.ObtenerLunar;

public class ObtenerInformacionLunarHandler : IRequestHandler<ObtenerInformacionLunarQuery, Result<InformacionLunarViewModel>>
{
    private readonly AppDbContext _db;

    public ObtenerInformacionLunarHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<InformacionLunarViewModel>> Handle(
        ObtenerInformacionLunarQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Ejecutar cálculo astronómico local para Caldono, Cauca
        var calculoLunar = ServicioCalculoLunar.CalcularFase();

        // 2. Asegurar que existan reglas lunares en base de datos
        var existenReglas = await _db.ReglasLunares.AnyAsync(cancellationToken);
        if (!existenReglas)
        {
            await SeedReglasLunaresPorDefectoAsync(cancellationToken);
        }

        // 3. Consultar reglas para la fase lunar actual
        var queryReglas = _db.ReglasLunares
            .Include(r => r.Cultivo)
            .AsNoTracking()
            .Where(r => r.FaseLunar == calculoLunar.FaseEnum);

        if (request.CultivoId.HasValue)
        {
            queryReglas = queryReglas.Where(r => r.CultivoId == null || r.CultivoId == request.CultivoId.Value);
        }

        var reglasDb = await queryReglas.ToListAsync(cancellationToken);

        // 4. Si no hay reglas específicas para esta fase, cargar todas las reglas de la fase sin filtro de cultivo
        if (!reglasDb.Any() && request.CultivoId.HasValue)
        {
            reglasDb = await _db.ReglasLunares
                .Include(r => r.Cultivo)
                .AsNoTracking()
                .Where(r => r.FaseLunar == calculoLunar.FaseEnum && r.CultivoId == null)
                .ToListAsync(cancellationToken);
        }

        // 5. Cargar las prácticas y recomendaciones de la fase tal como están registradas
        var practicasRecomendadas = reglasDb.Select(r => new PracticaLunarDto
        {
            Id = r.Id,
            FaseLunar = r.FaseLunar,
            Practica = r.Practica ?? "Labor Agrícola",
            IconoPractica = ObtenerIconoPractica(r.Practica ?? ""),
            Recomendacion = r.Recomendacion ?? "",
            CultivoId = r.CultivoId,
            NombreCultivo = r.Cultivo?.Nombre ?? "Todos los cultivos",
            IconoCultivo = r.Cultivo?.Icono ?? "🌿",
            NivelConfianza = r.NivelConfianza ?? "Alta",
            EsRecomendada = true
        }).ToList();

        var practicasNoRecomendadas = new List<PracticaLunarDto>();

        // 6. Obtener lista de cultivos para el selector de filtro (ÚNICAMENTE los cultivos que tienen reglas cargadas en ReglasLunares)
        var cultivosIdsConReglas = await _db.ReglasLunares
            .AsNoTracking()
            .Where(r => r.CultivoId != null)
            .Select(r => r.CultivoId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var cultivosDisponibles = cultivosIdsConReglas.Any()
            ? await _db.Cultivos
                .AsNoTracking()
                .Where(c => c.Activo && cultivosIdsConReglas.Contains(c.Id))
                .OrderBy(c => c.Nombre)
                .Select(c => new CultivoFiltroLunarDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Icono = c.Icono
                })
                .ToListAsync(cancellationToken)
            : new List<CultivoFiltroLunarDto>();

        string? nombreCultivoSel = null;
        if (request.CultivoId.HasValue)
        {
            nombreCultivoSel = cultivosDisponibles.FirstOrDefault(c => c.Id == request.CultivoId.Value)?.Nombre;
        }

        var cultureEs = new CultureInfo("es-CO");
        var viewModel = new InformacionLunarViewModel
        {
            FaseEnum = calculoLunar.FaseEnum,
            NombreFase = calculoLunar.NombreFase,
            Icono = calculoLunar.Icono,
            DescripcionVisual = calculoLunar.DescripcionVisual,
            Iluminacion = calculoLunar.Iluminacion,
            PorcentajeIluminacion = calculoLunar.PorcentajeIluminacion,
            EdadDias = calculoLunar.EdadDias,
            DiasParaProximoCambio = calculoLunar.DiasParaProximoCambio,
            HorasParaProximoCambio = calculoLunar.HorasParaProximoCambio,
            NombreProximaFase = calculoLunar.NombreProximaFase,
            FechaProximaFaseLocal = calculoLunar.FechaProximaFaseLocal,
            FechaProximaFormateada = calculoLunar.FechaProximaFaseLocal.ToString("dddd, dd 'de' MMMM 'a las' hh:mm tt", cultureEs),
            FechaActualLocal = calculoLunar.FechaCalculoLocal,
            FechaActualFormateada = calculoLunar.FechaCalculoLocal.ToString("dddd, dd 'de' MMMM 'de' yyyy", cultureEs),
            Ubicacion = calculoLunar.Ubicacion,
            CalendarioMes = calculoLunar.CalendarioMes,
            PracticasRecomendadas = practicasRecomendadas,
            PracticasNoRecomendadas = practicasNoRecomendadas,
            CultivosDisponibles = cultivosDisponibles,
            CultivoIdSeleccionado = request.CultivoId,
            NombreCultivoSeleccionado = nombreCultivoSel
        };

        return Result<InformacionLunarViewModel>.Success(viewModel);
    }

    private static string ObtenerIconoPractica(string practica)
    {
        if (string.IsNullOrWhiteSpace(practica)) return "🌱";
        var p = practica.ToLowerInvariant();
        if (p.Contains("miel") || p.Contains("abeja")) return "🍯";
        if (p.Contains("esqueje") || p.Contains("injert")) return "🌿";
        if (p.Contains("cabello") || p.Contains("pelo")) return "✂️";
        if (p.Contains("guadua") || p.Contains("mader") || p.Contains("bambu")) return "🎋";
        if (p.Contains("transplant") || p.Contains("trasplant")) return "🌱";
        if (p.Contains("cafe") || p.Contains("café")) return "☕";
        if (p.Contains("fruto") || p.Contains("cosech") || p.Contains("recolec")) return "🧺";
        if (p.Contains("siembr")) return "🌱";
        if (p.Contains("huerta") || p.Contains("abon")) return "🧪";
        if (p.Contains("medicinal") || p.Contains("planta")) return "☘️";
        if (p.Contains("limpieza") || p.Contains("terreno") || p.Contains("desyerb")) return "⛏️";
        return "✨";
    }

    private async Task SeedReglasLunaresPorDefectoAsync(CancellationToken ct)
    {
        var reglas = new List<ReglasLunares>
        {
            // ── CUARTO CRECIENTE (🌒) ────────────────────────────────────────────────
            new() {
                FaseLunar = FaseLunar.CuartoCrediente,
                Practica = "Cosecha de miel de abejas",
                Recomendacion = "Se puede recolectar miel de abejas",
                CultivoId = null,
                NivelConfianza = "Alta"
            },
            new() {
                FaseLunar = FaseLunar.CuartoCrediente,
                Practica = "Elaboración de esquejes",
                Recomendacion = "En esta luna se pueden elaborar esquejes",
                CultivoId = null,
                NivelConfianza = "Alta"
            },

            // ── CUARTO MENGUANTE (🌘) ────────────────────────────────────────────────
            new() {
                FaseLunar = FaseLunar.CuartoMenguante,
                Practica = "Corte de cabello",
                Recomendacion = "Corte de cabello para que crezca",
                CultivoId = null,
                NivelConfianza = "Alta"
            },
            new() {
                FaseLunar = FaseLunar.CuartoMenguante,
                Practica = "Corte de madera y Guadua",
                Recomendacion = "Cortar en horas de la mañana, 6:00AM (hora ideal)",
                CultivoId = null,
                NivelConfianza = "Alta"
            },
            new() {
                FaseLunar = FaseLunar.CuartoMenguante,
                Practica = "Transplante",
                Recomendacion = "Alto consenso entre los conocimientos de ASCAMPESINA y las fuentes revisadas; no hay divergencia.",
                CultivoId = null,
                NivelConfianza = "Alta"
            },

            // ── LUNA LLENA (🌕) ─────────────────────────────────────────────────────
            new() {
                FaseLunar = FaseLunar.LunaLlena,
                Practica = "Cosecha de cafe",
                Recomendacion = "El momento principal para cosechar el café es cuando la cereza alcanza su punto óptimo de maduración; sin embargo, si la cosecha coincide con la luna llena, el grano tiende a salir con mayor peso.",
                CultivoId = null,
                NivelConfianza = "Alta"
            },
            new() {
                FaseLunar = FaseLunar.LunaLlena,
                Practica = "Cosecha de frutos maduros",
                Recomendacion = "Cosechar frutas y hortalizas en su punto más jugoso",
                CultivoId = null,
                NivelConfianza = "Alta"
            },
            new() {
                FaseLunar = FaseLunar.LunaLlena,
                Practica = "Siembra general",
                Recomendacion = "Es donde coincide con más fuerza el testimonio tradicional de ASCAMPESINA y la documentación; evitar el cuarto creciente, que ambas fuentes tratan como fase \"de espera\".",
                CultivoId = null,
                NivelConfianza = "Alta"
            },

            // ── LUNA NUEVA (🌑) ─────────────────────────────────────────────────────
            new() {
                FaseLunar = FaseLunar.LunaNueva,
                Practica = "Abonar huertas",
                Recomendacion = "Solo se abonan huertas",
                CultivoId = null,
                NivelConfianza = "Alta"
            },
            new() {
                FaseLunar = FaseLunar.LunaNueva,
                Practica = "Cosecha de plantas medicinales",
                Recomendacion = "Solo se cosechan plantas medicinales",
                CultivoId = null,
                NivelConfianza = "Alta"
            },
            new() {
                FaseLunar = FaseLunar.LunaNueva,
                Practica = "Elaboración de abonos",
                Recomendacion = "Se pueden elaborar abonos organicos",
                CultivoId = null,
                NivelConfianza = "Alta"
            },
            new() {
                FaseLunar = FaseLunar.LunaNueva,
                Practica = "Limpieza y mantenimiento de terreno",
                Recomendacion = "Aprovechar para desyerbar y preparar el suelo",
                CultivoId = null,
                NivelConfianza = "Media"
            }
        };

        _db.ReglasLunares.AddRange(reglas);
        await _db.SaveChangesAsync(ct);
    }
}
