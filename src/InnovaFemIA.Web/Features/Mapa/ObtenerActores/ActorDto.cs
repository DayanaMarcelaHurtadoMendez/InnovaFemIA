namespace InnovaFemIA.Web.Features.Mapa.ObtenerActores;

/// <summary>
/// DTO de representación de un actor en el mapa (asociación o punto de compra).
/// </summary>
public class ActorDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string TipoActor { get; set; } = string.Empty; // "Asociacion" o "PuntoCompra"
    public string Descripcion { get; set; } = string.Empty;
    public string DatosContacto { get; set; } = string.Empty;
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public string? LogoBase64 { get; set; }
}
