using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Admin.AdminLogin;

public record AdminLoginCommand(string Usuario, string Contrasena) : IRequest<Result<AdminUsuarioDto>>;

public record AdminUsuarioDto(int Id, string NombreUsuario, string NombreCompleto, string Email);

public class AdminLoginHandler : IRequestHandler<AdminLoginCommand, Result<AdminUsuarioDto>>
{
    private readonly AppDbContext _db;

    public AdminLoginHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<AdminUsuarioDto>> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Contrasena))
        {
            return Result<AdminUsuarioDto>.BadRequest("Por favor ingrese usuario y contraseña.");
        }

        var admin = await _db.Administradores
            .FirstOrDefaultAsync(a => a.NombreUsuario == request.Usuario && a.Activo, cancellationToken);

        if (admin == null || !BCrypt.Net.BCrypt.Verify(request.Contrasena, admin.HashContrasena))
        {
            return Result<AdminUsuarioDto>.Unauthorized("Usuario o contraseña incorrectos.");
        }

        admin.UltimoLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<AdminUsuarioDto>.Success(new AdminUsuarioDto(
            admin.Id,
            admin.NombreUsuario,
            admin.NombreCompleto ?? admin.NombreUsuario,
            admin.Email ?? string.Empty
        ));
    }
}
