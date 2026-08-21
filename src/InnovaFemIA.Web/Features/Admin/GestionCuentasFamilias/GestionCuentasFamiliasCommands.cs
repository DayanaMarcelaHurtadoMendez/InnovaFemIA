using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Entities;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Admin.GestionCuentasFamilias;

// DTOs
public record CuentaFamiliaAdminDto(
    int Id,
    string NombreUsuario,
    int FamiliaId,
    string NombreFamilia,
    string Vereda,
    bool Activo,
    DateTime FechaCreacion,
    DateTime? UltimoLogin
);

// Queries & Commands
public record ObtenerCuentasFamiliasQuery : IRequest<Result<List<CuentaFamiliaAdminDto>>>;

public record CrearCuentaFamiliaCommand(
    string NombreUsuario,
    string Contrasena,
    int FamiliaId
) : IRequest<Result<int>>;

public record EditarCuentaFamiliaCommand(
    int Id,
    string? NuevaContrasena,
    bool Activo
) : IRequest<Result<bool>>;

public record EliminarCuentaFamiliaCommand(int Id) : IRequest<Result<bool>>;

// Handlers
public class GestionCuentasFamiliasHandler :
    IRequestHandler<ObtenerCuentasFamiliasQuery, Result<List<CuentaFamiliaAdminDto>>>,
    IRequestHandler<CrearCuentaFamiliaCommand, Result<int>>,
    IRequestHandler<EditarCuentaFamiliaCommand, Result<bool>>,
    IRequestHandler<EliminarCuentaFamiliaCommand, Result<bool>>
{
    private readonly AppDbContext _db;

    public GestionCuentasFamiliasHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<CuentaFamiliaAdminDto>>> Handle(ObtenerCuentasFamiliasQuery request, CancellationToken cancellationToken)
    {
        var cuentas = await _db.UsuariosFamilias
            .Include(u => u.Familia)
            .OrderBy(u => u.NombreUsuario)
            .Select(u => new CuentaFamiliaAdminDto(
                u.Id,
                u.NombreUsuario,
                u.FamiliaId,
                u.Familia != null ? u.Familia.Nombre : "Sin Familia",
                u.Familia != null ? u.Familia.Vereda : string.Empty,
                u.Activo,
                u.FechaCreacion,
                u.UltimoLogin
            ))
            .ToListAsync(cancellationToken);

        return Result<List<CuentaFamiliaAdminDto>>.Success(cuentas);
    }

    public async Task<Result<int>> Handle(CrearCuentaFamiliaCommand request, CancellationToken cancellationToken)
    {
        // Validaciones según Sección 9.5 y 9.8 de la documentación ejecutiva
        var username = request.NombreUsuario?.Trim();
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 50 || username.Contains(' '))
        {
            return Result<int>.BadRequest("El nombre de usuario debe tener entre 3 y 50 caracteres y no debe contener espacios.");
        }

        if (string.IsNullOrWhiteSpace(request.Contrasena) || request.Contrasena.Length < 6)
        {
            return Result<int>.BadRequest("La contraseña debe tener mínimo 6 caracteres.");
        }

        if (await _db.UsuariosFamilias.AnyAsync(u => u.NombreUsuario.ToLower() == username.ToLower(), cancellationToken))
        {
            return Result<int>.BadRequest("Este nombre de usuaria ya está en uso. Por favor elige uno diferente.");
        }

        var familia = await _db.Familias.FirstOrDefaultAsync(f => f.Id == request.FamiliaId, cancellationToken);
        if (familia == null) return Result<int>.NotFound("Familia no encontrada.");

        var usuario = new UsuarioFamilia
        {
            NombreUsuario = username,
            HashContrasena = BCrypt.Net.BCrypt.HashPassword(request.Contrasena, workFactor: 12),
            FamiliaId = request.FamiliaId,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _db.UsuariosFamilias.Add(usuario);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(usuario.Id);
    }

    public async Task<Result<bool>> Handle(EditarCuentaFamiliaCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _db.UsuariosFamilias.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (usuario == null) return Result<bool>.NotFound("Cuenta no encontrada.");

        usuario.Activo = request.Activo;

        if (!string.IsNullOrWhiteSpace(request.NuevaContrasena))
        {
            if (request.NuevaContrasena.Length < 6)
                return Result<bool>.BadRequest("La contraseña debe tener mínimo 6 caracteres.");

            usuario.HashContrasena = BCrypt.Net.BCrypt.HashPassword(request.NuevaContrasena, workFactor: 12);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> Handle(EliminarCuentaFamiliaCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _db.UsuariosFamilias.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (usuario == null) return Result<bool>.NotFound("Cuenta no encontrada.");

        _db.UsuariosFamilias.Remove(usuario);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
