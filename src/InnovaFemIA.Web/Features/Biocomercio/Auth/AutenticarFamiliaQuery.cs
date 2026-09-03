using InnovaFemIA.Web.Features.Shared.Persistence;
using InnovaFemIA.Web.Features.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Biocomercio.Auth;

public record AutenticarFamiliaQuery(string Usuario, string Contrasena) : IRequest<Result<FamiliaDto>>;

public class FamiliaDto
{
    public int Id { get; set; }
    public int FamiliaId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
}

public class AutenticarFamiliaQueryHandler : IRequestHandler<AutenticarFamiliaQuery, Result<FamiliaDto>>
{
    private readonly AppDbContext _db;

    public AutenticarFamiliaQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<FamiliaDto>> Handle(AutenticarFamiliaQuery request, CancellationToken cancellationToken)
    {
        var usuario = await _db.UsuariosFamilias
            .FirstOrDefaultAsync(u => u.NombreUsuario.ToLower() == request.Usuario.ToLower(), cancellationToken);

        if (usuario == null)
        {
            return Result<FamiliaDto>.Failure("Usuario o contraseña incorrectos");
        }

        if (!usuario.Activo)
        {
            return Result<FamiliaDto>.Failure("Usuario o contraseña incorrectos");
        }

        if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > DateTime.UtcNow)
        {
            return Result<FamiliaDto>.Failure("Demasiados intentos fallidos. Por favor espera 5 minutos antes de intentarlo de nuevo");
        }

        bool isValid = false;
        try
        {
            isValid = BCrypt.Net.BCrypt.Verify(request.Contrasena, usuario.HashContrasena);
        }
        catch
        {
            isValid = false;
        }

        if (!isValid)
        {
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= 3)
            {
                usuario.BloqueadoHasta = DateTime.UtcNow.AddMinutes(5);
                usuario.IntentosFallidos = 0; // Se reinicia para la proxima vez que se bloquee o inicie bien (aunque si se bloquea, despues del bloqueo estara en 0)
                await _db.SaveChangesAsync(cancellationToken);
                return Result<FamiliaDto>.Failure("Demasiados intentos fallidos. Por favor espera 5 minutos antes de intentarlo de nuevo");
            }

            await _db.SaveChangesAsync(cancellationToken);
            return Result<FamiliaDto>.Failure("Usuario o contraseña incorrectos");
        }

        // Exito
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        usuario.UltimoLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<FamiliaDto>.Success(new FamiliaDto
        {
            Id = usuario.Id,
            FamiliaId = usuario.FamiliaId,
            NombreUsuario = usuario.NombreUsuario
        });
    }
}
