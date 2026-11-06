using InnovaFemIA.Web.Features.Shared.Common;
using MediatR;

namespace InnovaFemIA.Web.Features.Biocomercio.Calculadoras;

public record ObtenerTiemposCosechaQuery : IRequest<Result<IEnumerable<CultivoTiempoDto>>>;

public class CultivoTiempoDto
{
    public int CultivoId { get; set; }
    public string NombreCultivo { get; set; } = string.Empty;
    public string Actividad { get; set; } = string.Empty;
    public int MesesHastaCosecha { get; set; }
    public string RangoCosechaTexto { get; set; } = string.Empty;
    public string FrecuenciaAbono { get; set; } = string.Empty;
    public string ManejoPoda { get; set; } = string.Empty;
    public string ProximaActividad { get; set; } = string.Empty;
    public string DescripcionTemplate { get; set; } = string.Empty;
    public string Fuente { get; set; } = string.Empty;
    public string Confianza { get; set; } = string.Empty;
}

public class ObtenerTiemposCosechaQueryHandler : IRequestHandler<ObtenerTiemposCosechaQuery, Result<IEnumerable<CultivoTiempoDto>>>
{
    public Task<Result<IEnumerable<CultivoTiempoDto>>> Handle(ObtenerTiemposCosechaQuery request, CancellationToken cancellationToken)
    {
        // HU-022: TABLA DE TIEMPOS DE REFERENCIA (van en el código) — cultivos principales de la comunidad
        var tiempos = new List<CultivoTiempoDto>
        {
            // ── CAFÉ (Fuente: Cenicafé / FNC - Confianza: Alta) ──
            new CultivoTiempoDto
            {
                CultivoId = 1,
                NombreCultivo = "Café",
                Actividad = "Siembra",
                MesesHastaCosecha = 18,
                RangoCosechaTexto = "18 meses (primera cosecha desde almácigo); luego 2 cosechas/año (principal oct-dic, mitaca abr-jun)",
                FrecuenciaAbono = "Fraccionado durante el año; sostenimiento cada 4 meses",
                ManejoPoda = "Poda de formación y renovación por zoca (según ciclo del árbol, ~5 años)",
                ProximaActividad = "Abonar: fraccionado durante el año; sostenimiento cada 4 meses.",
                DescripcionTemplate = "Tu café sembrado el {fecha} estaría listo hacia {mes} de {año}. Recuerda abonar cada 4 meses.",
                Fuente = "Cenicafé / FNC",
                Confianza = "Alta"
            },
            new CultivoTiempoDto
            {
                CultivoId = 1,
                NombreCultivo = "Café",
                Actividad = "Abonado",
                MesesHastaCosecha = 6,
                RangoCosechaTexto = "2 cosechas/año (principal oct-dic, mitaca abr-jun)",
                FrecuenciaAbono = "Fraccionado durante el año; sostenimiento cada 4 meses",
                ManejoPoda = "Poda de formación y renovación por zoca (según ciclo del árbol, ~5 años)",
                ProximaActividad = "Volver a abonar en 4 meses (fraccionado durante el año; sostenimiento cada 4 meses).",
                DescripcionTemplate = "Abonado de café registrado el {fecha}. Recuerda volver a abonar en 4 meses.",
                Fuente = "Cenicafé / FNC",
                Confianza = "Alta"
            },
            new CultivoTiempoDto
            {
                CultivoId = 1,
                NombreCultivo = "Café",
                Actividad = "Poda",
                MesesHastaCosecha = 18,
                RangoCosechaTexto = "Renovación por zoca (según ciclo del árbol, ~5 años)",
                FrecuenciaAbono = "Fraccionado durante el año; sostenimiento cada 4 meses",
                ManejoPoda = "Poda de formación y renovación por zoca (según ciclo del árbol, ~5 años)",
                ProximaActividad = "Poda de formación y renovación por zoca (según ciclo del árbol, ~5 años).",
                DescripcionTemplate = "Poda de café registrada el {fecha}. Recuerda: poda de formación y renovación por zoca (según ciclo del árbol, ~5 años).",
                Fuente = "Cenicafé / FNC",
                Confianza = "Alta"
            },

            // ── PLÁTANO (Fuente: Agrosavia - Confianza: Alta) ──
            new CultivoTiempoDto
            {
                CultivoId = 2,
                NombreCultivo = "Plátano",
                Actividad = "Siembra",
                MesesHastaCosecha = 12,
                RangoCosechaTexto = "11 a 14 meses según altitud",
                FrecuenciaAbono = "25% de la dosis cada 4 meses; materia orgánica cada 3 meses",
                ManejoPoda = "Deshoje y deshije periódico",
                ProximaActividad = "Abonar: 25% de la dosis cada 4 meses; materia orgánica cada 3 meses.",
                DescripcionTemplate = "Tu plátano sembrado el {fecha} estaría listo hacia {mes} de {año}. Recuerda abonar cada 4 meses (o materia orgánica cada 3 meses).",
                Fuente = "Agrosavia",
                Confianza = "Alta"
            },
            new CultivoTiempoDto
            {
                CultivoId = 2,
                NombreCultivo = "Plátano",
                Actividad = "Abonado",
                MesesHastaCosecha = 6,
                RangoCosechaTexto = "11 a 14 meses según altitud",
                FrecuenciaAbono = "25% de la dosis cada 4 meses; materia orgánica cada 3 meses",
                ManejoPoda = "Deshoje y deshije periódico",
                ProximaActividad = "Volver a abonar en 4 meses (25% de la dosis cada 4 meses; materia orgánica cada 3 meses).",
                DescripcionTemplate = "Abonado de plátano registrado el {fecha}. Recuerda volver a abonar en 4 meses (o materia orgánica cada 3 meses).",
                Fuente = "Agrosavia",
                Confianza = "Alta"
            },
            new CultivoTiempoDto
            {
                CultivoId = 2,
                NombreCultivo = "Plátano",
                Actividad = "Poda",
                MesesHastaCosecha = 4,
                RangoCosechaTexto = "Deshoje y deshije periódico",
                FrecuenciaAbono = "25% de la dosis cada 4 meses; materia orgánica cada 3 meses",
                ManejoPoda = "Deshoje y deshije periódico",
                ProximaActividad = "Deshoje y deshije periódico.",
                DescripcionTemplate = "Labor de plátano registrada el {fecha}. Recuerda: deshoje y deshije periódico.",
                Fuente = "Agrosavia",
                Confianza = "Alta"
            },

            // ── YUCA (Fuente: Infoagro / Agrosavia - Confianza: Media) ──
            new CultivoTiempoDto
            {
                CultivoId = 3,
                NombreCultivo = "Yuca",
                Actividad = "Siembra",
                MesesHastaCosecha = 8,
                RangoCosechaTexto = "7 a 10 meses (consumo); hasta 12-24 para industria",
                FrecuenciaAbono = "Al inicio del ciclo; refuerzo según análisis de suelo",
                ManejoPoda = "No requiere poda tradicional",
                ProximaActividad = "Abonar al inicio del ciclo; refuerzo según análisis de suelo.",
                DescripcionTemplate = "Tu yuca sembrada el {fecha} estaría lista hacia {mes} de {año}. Recuerda abonar al inicio del ciclo.",
                Fuente = "Infoagro / Agrosavia",
                Confianza = "Media"
            },
            new CultivoTiempoDto
            {
                CultivoId = 3,
                NombreCultivo = "Yuca",
                Actividad = "Abonado",
                MesesHastaCosecha = 5,
                RangoCosechaTexto = "7 a 10 meses (consumo); hasta 12-24 para industria",
                FrecuenciaAbono = "Al inicio del ciclo; refuerzo según análisis de suelo",
                ManejoPoda = "No requiere poda tradicional",
                ProximaActividad = "Refuerzo según análisis de suelo.",
                DescripcionTemplate = "Abonado de yuca registrado el {fecha}. Recuerda aplicar refuerzo según análisis de suelo.",
                Fuente = "Infoagro / Agrosavia",
                Confianza = "Media"
            },
            new CultivoTiempoDto
            {
                CultivoId = 3,
                NombreCultivo = "Yuca",
                Actividad = "Poda",
                MesesHastaCosecha = 6,
                RangoCosechaTexto = "No requiere poda tradicional",
                FrecuenciaAbono = "Al inicio del ciclo; refuerzo según análisis de suelo",
                ManejoPoda = "No requiere poda tradicional",
                ProximaActividad = "No requiere poda tradicional.",
                DescripcionTemplate = "Labor de yuca registrada el {fecha}. No requiere poda tradicional.",
                Fuente = "Infoagro / Agrosavia",
                Confianza = "Media"
            },

            // ── AGUACATE (Fuente: DANE-SIPSA / Ficha técnica - Confianza: Alta) ──
            new CultivoTiempoDto
            {
                CultivoId = 4,
                NombreCultivo = "Aguacate",
                Actividad = "Siembra",
                MesesHastaCosecha = 14,
                RangoCosechaTexto = "12 a 18 meses según altitud y manejo",
                FrecuenciaAbono = "Fraccionado durante el año; cada 4 meses",
                ManejoPoda = "Poda de formación y mantenimiento sanitario tras cosecha",
                ProximaActividad = "Abonar: fraccionado cada 4 meses; análisis de suelo.",
                DescripcionTemplate = "Tu aguacate sembrado el {fecha} estaría listo hacia {mes} de {año}. Recuerda abonar de forma fraccionada cada 4 meses.",
                Fuente = "DANE-SIPSA / Ficha técnica",
                Confianza = "Alta"
            },
            new CultivoTiempoDto
            {
                CultivoId = 4,
                NombreCultivo = "Aguacate",
                Actividad = "Abonado",
                MesesHastaCosecha = 6,
                RangoCosechaTexto = "Cosechas anuales según piso térmico",
                FrecuenciaAbono = "Fraccionado durante el año; cada 4 meses",
                ManejoPoda = "Poda de formación y mantenimiento sanitario tras cosecha",
                ProximaActividad = "Volver a abonar en 4 meses (fraccionado durante el año).",
                DescripcionTemplate = "Abonado de aguacate registrado el {fecha}. Recuerda volver a abonar en 4 meses.",
                Fuente = "DANE-SIPSA / Ficha técnica",
                Confianza = "Alta"
            },
            new CultivoTiempoDto
            {
                CultivoId = 4,
                NombreCultivo = "Aguacate",
                Actividad = "Poda",
                MesesHastaCosecha = 8,
                RangoCosechaTexto = "Poda de formación y mantenimiento tras la cosecha",
                FrecuenciaAbono = "Fraccionado durante el año; cada 4 meses",
                ManejoPoda = "Poda de formación y mantenimiento sanitario tras cosecha",
                ProximaActividad = "Poda de mantenimiento sanitario y aclareo tras cosecha.",
                DescripcionTemplate = "Poda de aguacate registrada el {fecha}. Realizar mantenimiento sanitario tras cosechar.",
                Fuente = "DANE-SIPSA / Ficha técnica",
                Confianza = "Alta"
            }
        };

        return Task.FromResult(Result<IEnumerable<CultivoTiempoDto>>.Success(tiempos));
    }
}
