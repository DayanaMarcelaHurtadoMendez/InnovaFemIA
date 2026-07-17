namespace InnovaFemIA.Web.Features.Shared.Attributes;

/// <summary>
/// Marca un endpoint de API como accesible públicamente (desde fuera del sistema).
/// Los endpoints SIN este atributo son de uso interno exclusivo y retornan 403 Forbidden
/// cuando se accede desde fuera.
///
/// Los endpoints CON este atributo requieren una API Key válida enviada en:
/// - Header: X-Api-Key: {clave}
/// - Query string: ?apikey={clave}
///
/// El middleware ApiKeyMiddleware valida la clave contra la tabla ApiKeys en MySQL.
///
/// Uso:
/// <code>
/// [HttpGet("familias")]
/// [PublicApi]
/// public async Task&lt;IActionResult&gt; ObtenerFamilias() { ... }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class PublicApiAttribute : Attribute { }
