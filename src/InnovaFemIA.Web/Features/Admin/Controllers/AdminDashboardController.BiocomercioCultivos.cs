using InnovaFemIA.Web.Features.Admin.GestionCultivos;
using InnovaFemIA.Web.Features.Admin.GestionReglasLunares;
using InnovaFemIA.Web.Features.Admin.GestionStockConsolidado;
using InnovaFemIA.Web.Features.Admin.GestionTiemposCosecha;
using InnovaFemIA.Web.Features.Shared.Entities;
using Microsoft.AspNetCore.Mvc;

namespace InnovaFemIA.Web.Features.Admin.Controllers;

public partial class AdminDashboardController
{
    // ── GESTIÓN DE CULTIVOS DEL SISTEMA (ADMIN) ──────────────────────────────
    [HttpGet("cultivos")]
    public async Task<IActionResult> Cultivos()
    {
        var result = await _mediator.Send(new ObtenerCultivosAdminQuery());
        return View("~/Views/Admin/Cultivos.cshtml", result.Value);
    }

    [HttpPost("cultivos/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCultivo([FromForm] CrearCultivoCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Cultivo registrado exitosamente.";
        return RedirectToAction("Cultivos");
    }

    [HttpPost("cultivos/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarCultivo([FromForm] EditarCultivoCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Cultivo actualizado exitosamente.";
        return RedirectToAction("Cultivos");
    }

    [HttpPost("cultivos/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCultivo([FromForm] int id)
    {
        var result = await _mediator.Send(new EliminarCultivoCommand(id));
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Cultivo eliminado exitosamente.";
        return RedirectToAction("Cultivos");
    }

    // ── MÓDULO 1: GESTIÓN DE REGLAS Y PRÁCTICAS LUNARES ──────────────────────
    [HttpGet("reglas-lunares")]
    public async Task<IActionResult> ReglasLunares(FaseLunar? fase)
    {
        var result = await _mediator.Send(new ObtenerReglasLunaresAdminQuery(fase));
        var cultivosResult = await _mediator.Send(new Mapa.ObtenerCultivos.ObtenerCultivosQuery());
        
        ViewBag.Cultivos = cultivosResult.Value ?? [];
        ViewBag.FaseSeleccionada = fase;
        return View("~/Views/Admin/ReglasLunares.cshtml", result.Value);
    }

    [HttpPost("reglas-lunares/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearReglaLunar([FromForm] CrearReglaLunarCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Práctica lunar agregada exitosamente.";
        return RedirectToAction("ReglasLunares");
    }

    [HttpPost("reglas-lunares/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarReglaLunar([FromForm] EditarReglaLunarCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Práctica lunar actualizada exitosamente.";
        return RedirectToAction("ReglasLunares");
    }

    [HttpPost("reglas-lunares/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarReglaLunar([FromForm] int id)
    {
        var result = await _mediator.Send(new EliminarReglaLunarCommand(id));
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Práctica lunar eliminada exitosamente.";
        return RedirectToAction("ReglasLunares");
    }

    // ── MÓDULO 3: VISTA CONSOLIDADA DE STOCK ─────────────────────────────────
    [HttpGet("biocomercio/stock")]
    public async Task<IActionResult> BiocomercioStock()
    {
        var result = await _mediator.Send(new ObtenerStockConsolidadoQuery());
        return View("~/Views/Admin/BiocomercioStock.cshtml", result.Value);
    }

    // ── MÓDULO 3: TIEMPOS ESTÁNDAR DE COSECHA (CALCULADORA) ──────────────────
    [HttpGet("biocomercio/tiempos")]
    public async Task<IActionResult> BiocomercioTiempos()
    {
        var result = await _mediator.Send(new ObtenerTiemposCosechaAdminQuery());
        var cultivosResult = await _mediator.Send(new Mapa.ObtenerCultivos.ObtenerCultivosQuery());
        
        ViewBag.Cultivos = cultivosResult.Value ?? [];
        return View("~/Views/Admin/BiocomercioTiempos.cshtml", result.Value);
    }

    [HttpPost("biocomercio/tiempos/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearTiempoCosecha([FromForm] CrearTiempoCosechaCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Tiempo estándar de cosecha registrado exitosamente.";
        return RedirectToAction("BiocomercioTiempos");
    }

    [HttpPost("biocomercio/tiempos/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarTiempoCosecha([FromForm] EditarTiempoCosechaCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Tiempo estándar actualizado exitosamente.";
        return RedirectToAction("BiocomercioTiempos");
    }

    [HttpPost("biocomercio/tiempos/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarTiempoCosecha([FromForm] int id)
    {
        var result = await _mediator.Send(new EliminarTiempoCosechaCommand(id));
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Registro eliminado exitosamente.";
        return RedirectToAction("BiocomercioTiempos");
    }
}
