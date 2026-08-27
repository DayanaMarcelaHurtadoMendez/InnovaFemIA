using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Entities;
using InnovaFemIA.Web.Features.Shared.Services;
using MediatR;

namespace InnovaFemIA.Web.Features.Mapa.ObtenerLunar;

/// <summary>
/// Query para obtener la información lunar astronómica actual de Caldono, Cauca
/// junto con las prácticas agrícolas recomendadas y no recomendadas desde la base de datos.
/// </summary>
public record ObtenerInformacionLunarQuery(int? CultivoId = null) : IRequest<Result<InformacionLunarViewModel>>;

public class InformacionLunarViewModel
{
    public FaseLunar FaseEnum { get; set; }
    public string NombreFase { get; set; } = string.Empty;
    public string Icono { get; set; } = "🌒";
    public string DescripcionVisual { get; set; } = string.Empty;
    public decimal Iluminacion { get; set; }
    public string PorcentajeIluminacion { get; set; } = "0%";
    public decimal EdadDias { get; set; }
    public decimal DiasParaProximoCambio { get; set; }
    public int HorasParaProximoCambio { get; set; }
    public string NombreProximaFase { get; set; } = string.Empty;
    public DateTime FechaProximaFaseLocal { get; set; }
    public string FechaProximaFormateada { get; set; } = string.Empty;
    public DateTime FechaActualLocal { get; set; }
    public string FechaActualFormateada { get; set; } = string.Empty;
    public UbicacionObservacion Ubicacion { get; set; } = new();
    public List<FaseCalendarioItem> CalendarioMes { get; set; } = [];
    public List<PracticaLunarDto> PracticasRecomendadas { get; set; } = [];
    public List<PracticaLunarDto> PracticasNoRecomendadas { get; set; } = [];
    public List<CultivoFiltroLunarDto> CultivosDisponibles { get; set; } = [];
    public int? CultivoIdSeleccionado { get; set; }
    public string? NombreCultivoSeleccionado { get; set; }
}

public class PracticaLunarDto
{
    public int Id { get; set; }
    public FaseLunar FaseLunar { get; set; }
    public string Practica { get; set; } = string.Empty;
    public string IconoPractica { get; set; } = "🌱";
    public string Recomendacion { get; set; } = string.Empty;
    public int? CultivoId { get; set; }
    public string NombreCultivo { get; set; } = "Todos los cultivos";
    public string IconoCultivo { get; set; } = "🌿";
    public string NivelConfianza { get; set; } = "Alta";
    public bool EsRecomendada { get; set; } = true;
}

public class CultivoFiltroLunarDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Icono { get; set; } = "🌱";
}
