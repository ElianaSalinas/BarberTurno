using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BarberTurno.Api.Data;
using BarberTurno.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BarberTurno.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>Expone el inicio de sesión y registro que crean la sesión JWT de la aplicación.</summary>
public class AuthController : ControllerBase
{
    private readonly ApiDbContext _context;
    private readonly IConfiguration _configuration;

    /// <summary>Recibe el contexto de datos y la configuración usada para firmar los tokens.</summary>
    public AuthController(ApiDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    /// <summary>Valida credenciales y devuelve un token JWT junto con el perfil del usuario.</summary>
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { mensaje = "Usuario y contraseña requeridos." });
        }

        // Busca el usuario persistido antes de verificar su hash BCrypt.
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Usuario.ToLower() == request.Usuario.Trim().ToLower());

        if (usuario == null)
        {
            return Unauthorized(new { mensaje = "Credenciales incorrectas." });
        }

        bool passwordValida = false;
        try
        {
            passwordValida = BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash);
        }
        catch
        {
            passwordValida = false;
        }

        if (!passwordValida)
        {
            return Unauthorized(new { mensaje = "Credenciales incorrectas." });
        }

        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Falta la configuración Jwt:Key.");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "BarberTurnoApi";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "BarberTurnoApp";
        var expiraEnUtc = DateTime.UtcNow.AddHours(24);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtKey);

        // El token conecta las peticiones posteriores con el usuario y su rol.
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("UsuarioId", usuario.Id.ToString())
            }),
            Expires = expiraEnUtc,
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(securityToken);

        int? barberoId = null;
        if (usuario.Rol.Equals("Barbero", StringComparison.OrdinalIgnoreCase))
        {
            var barbero = await _context.Barberos.FirstOrDefaultAsync(b => b.UsuarioId == usuario.Id);
            barberoId = barbero?.Id;
        }

        return Ok(new LoginResponseDto
        {
            Token = tokenString,
            ExpiraEnUtc = expiraEnUtc,
            Rol = usuario.Rol,
            UsuarioId = usuario.Id,
            Nombre = usuario.Nombre,
            BarberoId = barberoId
        });
    }

    [HttpPost("register")]
    /// <summary>Crea un usuario y devuelve la sesión JWT que la aplicación guarda localmente.</summary>
    public async Task<ActionResult<LoginResponseDto>> Register([FromBody] RegistroRequestDto request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest(new { mensaje = "Nombre, usuario y contraseña son requeridos." });
        }

        var existe = await _context.Usuarios
            .AnyAsync(u => u.Usuario.ToLower() == request.Usuario.Trim().ToLower());

        if (existe)
        {
            return Conflict(new { mensaje = "El usuario ya está registrado." });
        }

        var rol = request.Rol.Equals("Barbero", StringComparison.OrdinalIgnoreCase) ? "Barbero" : "Cliente";

        var nuevoUsuario = new BarberTurno.Api.Domain.UsuarioEntity
        {
            Nombre = request.Nombre.Trim(),
            Telefono = request.Telefono?.Trim() ?? string.Empty,
            Usuario = request.Usuario.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Rol = rol
        };

        _context.Usuarios.Add(nuevoUsuario);
        await _context.SaveChangesAsync();

        int? barberoId = null;
        if (rol == "Barbero")
        {
            // Un registro de barbero necesita además el perfil que usan citas y disponibilidad.
            var barbero = new BarberTurno.Api.Domain.BarberoEntity
            {
                Nombre = nuevoUsuario.Nombre,
                UsuarioId = nuevoUsuario.Id
            };
            _context.Barberos.Add(barbero);
            await _context.SaveChangesAsync();
            barberoId = barbero.Id;
        }

        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Falta la configuración Jwt:Key.");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "BarberTurnoApi";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "BarberTurnoApp";
        var expiraEnUtc = DateTime.UtcNow.AddHours(24);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, nuevoUsuario.Id.ToString()),
                new Claim(ClaimTypes.Name, nuevoUsuario.Nombre),
                new Claim(ClaimTypes.Role, nuevoUsuario.Rol),
                new Claim("UsuarioId", nuevoUsuario.Id.ToString())
            }),
            Expires = expiraEnUtc,
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(securityToken);

        return Ok(new LoginResponseDto
        {
            Token = tokenString,
            ExpiraEnUtc = expiraEnUtc,
            Rol = nuevoUsuario.Rol,
            UsuarioId = nuevoUsuario.Id,
            Nombre = nuevoUsuario.Nombre,
            BarberoId = barberoId
        });
    }
}
