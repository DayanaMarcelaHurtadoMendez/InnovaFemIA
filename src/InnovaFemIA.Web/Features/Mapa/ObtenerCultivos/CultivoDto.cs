namespace InnovaFemIA.Web.Features.Mapa.ObtenerCultivos;

/// <summary>
/// Objeto de transferencia de datos para selectores de filtrado cartográfico y reglas lunares.
/// </summary>
public class CultivoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Icono { get; set; } = "🌱";
}
