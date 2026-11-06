using InnovaFemIA.Web.Features.Shared.Common;
using MediatR;

namespace InnovaFemIA.Web.Features.Mapa.ObtenerDetalleFamilia;

/// <summary>
/// Query para obtener la ficha técnica completa y stock de una familia campesina.
/// </summary>
public record ObtenerDetalleFamiliaQuery(int FamiliaId) : IRequest<Result<DetalleFamiliaDto>>;

public class DetalleFamiliaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Vereda { get; set; } = string.Empty;
    public string Municipio { get; set; } = "Caldono";
    public string NombreContacto { get; set; } = string.Empty;
    public string TelefonoContacto { get; set; } = string.Empty;
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public string? Descripcion { get; set; }
    public string? FotoBase64 { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<StockDetalleDto> StockProductos { get; set; } = [];
}

public class StockDetalleDto
{
    public int CultivoId { get; set; }
    public string NombreCultivo { get; set; } = string.Empty;
    public string Icono { get; set; } = "🌱";
    public bool Disponible { get; set; }
    public DateTime UltimaActualizacion { get; set; }
    public string? Notas { get; set; }
}
