namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// Reglas de prácticas agrícolas recomendadas según la fase lunar.
/// Gestionadas por el administrador. Consultadas por el botón "lunita" del mapa
/// (Módulo 1) y por el chatbot lunar (Módulo 2).
/// </summary>
public class ReglasLunares
{
    public int Id { get; set; }

    /// <summary>
    /// Fase lunar a la que aplica la regla.
    /// Valores: "LunaNueva", "CuartoCrediente", "LunaLlena", "CuartoMenguante".
    /// </summary>
    public FaseLunar FaseLunar { get; set; }

    /// <summary>
    /// Práctica agrícola recomendada en esta fase
    /// (ej. "Sembrar", "Cosechar", "Abonar", "CortarGuadua", "Podar").
    /// </summary>
    public string Practica { get; set; } = string.Empty;

    /// <summary>Descripción detallada de la recomendación en lenguaje sencillo.</summary>
    public string Recomendacion { get; set; } = string.Empty;

    /// <summary>
    /// Cultivo específico al que aplica esta regla.
    /// Null = aplica a todos los cultivos.
    /// </summary>
    public int? CultivoId { get; set; }
    public Cultivo? Cultivo { get; set; }

    /// <summary>
    /// Nivel de confianza de la recomendación: "Alta", "Media", "Baja".
    /// Se muestra como indicador visual en el panel lunar.
    /// </summary>
    public string NivelConfianza { get; set; } = "Alta";
}

/// <summary>Fases lunares reconocidas por el sistema.</summary>
public enum FaseLunar
{
    LunaNueva = 0,
    CuartoCrediente = 1,
    LunaLlena = 2,
    CuartoMenguante = 3
}
