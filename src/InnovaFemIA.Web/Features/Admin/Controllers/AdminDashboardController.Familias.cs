using InnovaFemIA.Web.Features.Admin.GestionActores;
using InnovaFemIA.Web.Features.Admin.GestionFamilias;
using InnovaFemIA.Web.Features.Shared.Entities;
using Microsoft.AspNetCore.Mvc;

namespace InnovaFemIA.Web.Features.Admin.Controllers;

public partial class AdminDashboardController
{
    // ── MÓDULO 1: GESTIÓN DE FAMILIAS ASCAMPESINA ────────────────────────────
    [HttpGet("familias")]
    public async Task<IActionResult> Familias(string? busqueda)
    {
        var result = await _mediator.Send(new ObtenerFamiliasAdminQuery(busqueda));
        var cultivosResult = await _mediator.Send(new Mapa.ObtenerCultivos.ObtenerCultivosQuery());
        
        ViewBag.Cultivos = cultivosResult.Value ?? [];
        ViewBag.Busqueda = busqueda;
        return View("~/Views/Admin/Familias.cshtml", result.Value);
    }

    [HttpPost("familias/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearFamilia(
        [FromForm] string nombre,
        [FromForm] string vereda,
        [FromForm] string municipio,
        [FromForm] string nombreContacto,
        [FromForm] string telefonoContacto,
        [FromForm] double latitud,
        [FromForm] double longitud,
        [FromForm] string? descripcion,
        [FromForm] List<int>? cultivosIds,
        IFormFile? foto)
    {
        byte[]? fotoBytes = null;
        string? fotoMimeType = null;

        if (foto != null && foto.Length > 0)
        {
            using var ms = new MemoryStream();
            await foto.CopyToAsync(ms);
            fotoBytes = ms.ToArray();
            fotoMimeType = foto.ContentType;
        }

        var command = new CrearFamiliaAdminCommand(
            nombre,
            vereda,
            municipio,
            nombreContacto,
            telefonoContacto,
            latitud,
            longitud,
            descripcion,
            cultivosIds ?? [],
            fotoBytes,
            fotoMimeType
        );

        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Familia creada exitosamente.";
        return RedirectToAction("Familias");
    }

    [HttpPost("familias/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarFamilia(
        [FromForm] int id,
        [FromForm] string nombre,
        [FromForm] string vereda,
        [FromForm] string municipio,
        [FromForm] string nombreContacto,
        [FromForm] string telefonoContacto,
        [FromForm] double latitud,
        [FromForm] double longitud,
        [FromForm] string? descripcion,
        [FromForm] bool activo,
        [FromForm] List<int>? cultivosIds,
        IFormFile? foto)
    {
        byte[]? fotoBytes = null;
        string? fotoMimeType = null;

        if (foto != null && foto.Length > 0)
        {
            using var ms = new MemoryStream();
            await foto.CopyToAsync(ms);
            fotoBytes = ms.ToArray();
            fotoMimeType = foto.ContentType;
        }

        var command = new EditarFamiliaAdminCommand(
            id,
            nombre,
            vereda,
            municipio,
            nombreContacto,
            telefonoContacto,
            latitud,
            longitud,
            descripcion,
            activo,
            cultivosIds ?? [],
            fotoBytes,
            fotoMimeType
        );

        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Familia actualizada exitosamente.";
        return RedirectToAction("Familias");
    }

    [HttpPost("familias/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarFamilia([FromForm] int id)
    {
        var result = await _mediator.Send(new EliminarFamiliaAdminCommand(id));
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Familia eliminada exitosamente.";
        return RedirectToAction("Familias");
    }

    // ── MÓDULO 1: GESTIÓN DE ACTORES DEL MAPA ────────────────────────────────
    [HttpGet("actores")]
    public async Task<IActionResult> Actores(TipoActorMapa? tipo)
    {
        var result = await _mediator.Send(new ObtenerActoresAdminQuery(tipo));
        ViewBag.TipoSeleccionado = tipo;
        return View("~/Views/Admin/Actores.cshtml", result.Value);
    }

    [HttpPost("actores/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearActor([FromForm] CrearActorAdminCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Actor del mapa registrado exitosamente.";
        return RedirectToAction("Actores");
    }

    [HttpPost("actores/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarActor([FromForm] EditarActorAdminCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Actor del mapa actualizado exitosamente.";
        return RedirectToAction("Actores");
    }

    [HttpPost("actores/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarActor([FromForm] int id)
    {
        var result = await _mediator.Send(new EliminarActorAdminCommand(id));
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Actor eliminado exitosamente.";
        return RedirectToAction("Actores");
    }
}
