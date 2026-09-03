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
    public string DescripcionTemplate { get; set; } = string.Empty;
}

public class ObtenerTiemposCosechaQueryHandler : IRequestHandler<ObtenerTiemposCosechaQuery, Result<IEnumerable<CultivoTiempoDto>>>
{
    public Task<Result<IEnumerable<CultivoTiempoDto>>> Handle(ObtenerTiemposCosechaQuery request, CancellationToken cancellationToken)
    {
        // HU-022: Tiempos estándar de cosecha limitados a 3 productos clave definidos en código fuente
        var tiempos = new List<CultivoTiempoDto>
        {
            new CultivoTiempoDto { CultivoId = 1, NombreCultivo = "Café", Actividad = "Siembra", MesesHastaCosecha = 8, DescripcionTemplate = "Tu café sembrado el {fecha} estaría listo para cosechar aproximadamente en {mes} de {año}" },
            new CultivoTiempoDto { CultivoId = 2, NombreCultivo = "Plátano", Actividad = "Siembra", MesesHastaCosecha = 9, DescripcionTemplate = "Tu plátano sembrado el {fecha} estaría listo para cosechar aproximadamente en {mes} de {año}" },
            new CultivoTiempoDto { CultivoId = 3, NombreCultivo = "Aguacate", Actividad = "Siembra", MesesHastaCosecha = 14, DescripcionTemplate = "Tu aguacate sembrado el {fecha} estaría listo para cosechar aproximadamente en {mes} de {año}" }
        };

        return Task.FromResult(Result<IEnumerable<CultivoTiempoDto>>.Success(tiempos));
    }
}
