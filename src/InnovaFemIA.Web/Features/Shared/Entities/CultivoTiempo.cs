namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// Tiempos estándar de cosecha por cultivo y actividad.
/// Usados por la Calculadora de Cosecha del Módulo 3 (Biocomercio).
/// Gestionados desde el panel de administración.
/// </summary>
public class CultivoTiempo
{
    public int Id { get; set; }

    public int CultivoId { get; set; }
    public Cultivo Cultivo { get; set; } = null!;

    /// <summary>
    /// Actividad agrícola de referencia.
    /// Valores: "Siembra", "Abonado", "Poda", "AplicacionAgroquimico".
    /// </summary>
    public ActividadAgricola Actividad { get; set; }

    /// <summary>
    /// Número de meses hasta que el resultado de la actividad sea visible/cosechable.
    /// Ej: Café + Siembra = 8 meses para primera cosecha.
    /// </summary>
    public int MesesHastaCosecha { get; set; }

    /// <summary>
    /// Descripción en lenguaje sencillo para mostrar al usuario.
    /// Ej: "Tu café sembrado estará listo para la primera cosecha en aproximadamente 8 meses."
    /// </summary>
    public string DescripcionTemplate { get; set; } = string.Empty;

    /// <summary>Notas adicionales del agrónomo o investigador.</summary>
    public string? Notas { get; set; }
}

/// <summary>Actividades agrícolas disponibles en la calculadora de cosecha.</summary>
public enum ActividadAgricola
{
    Siembra = 1,
    Abonado = 2,
    Poda = 3,
    AplicacionAgroquimico = 4
}
