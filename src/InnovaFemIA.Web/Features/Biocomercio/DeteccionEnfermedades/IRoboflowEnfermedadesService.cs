namespace InnovaFemIA.Web.Features.Biocomercio.DeteccionEnfermedades;

public interface IRoboflowEnfermedadesService
{
    Task<ResultadoDeteccionDto> AnalizarImagenAsync(string base64Image, string? nombreArchivo = null);
}
