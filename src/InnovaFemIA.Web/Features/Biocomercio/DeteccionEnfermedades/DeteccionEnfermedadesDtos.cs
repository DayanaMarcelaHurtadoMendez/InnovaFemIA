namespace InnovaFemIA.Web.Features.Biocomercio.DeteccionEnfermedades;

public class DeteccionRequest
{
    public string? ImagenBase64 { get; set; }
    public string? NombreArchivo { get; set; }
}

public class PrediccionCajaDto
{
    public string Clase { get; set; } = string.Empty;
    public double Confianza { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Ancho { get; set; }
    public double Alto { get; set; }
}

public class ResultadoDeteccionDto
{
    public bool Exito { get; set; } = true;
    public string? Mensaje { get; set; }
    public bool EsModoDemostracion { get; set; }
    public string? MensajeDemostracion { get; set; }
    public string Diagnostico { get; set; } = string.Empty;
    public string NombreCientifico { get; set; } = string.Empty;
    public string EstadoSalud { get; set; } = "Sana"; // "Sana", "Alerta", "Critico"
    public double Confianza { get; set; }
    public string DescripcionSintomas { get; set; } = string.Empty;
    public List<string> RecomendacionesAgroecologicas { get; set; } = new();
    public List<PrediccionCajaDto> Detecciones { get; set; } = new();
}
