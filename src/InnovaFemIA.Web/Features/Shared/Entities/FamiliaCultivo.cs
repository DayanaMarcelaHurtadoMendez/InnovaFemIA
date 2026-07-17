namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// Tabla pivot (N:M) que relaciona una Familia con los Cultivos que produce.
/// Permite consultar qué familias producen un cultivo específico (filtro del mapa).
/// </summary>
public class FamiliaCultivo
{
    public int FamiliaId { get; set; }
    public Familia Familia { get; set; } = null!;

    public int CultivoId { get; set; }
    public Cultivo Cultivo { get; set; } = null!;
}
