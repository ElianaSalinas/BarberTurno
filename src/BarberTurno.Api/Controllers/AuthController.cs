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
public class AuthController : ControllerBase
{
    private readonly ApiDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(ApiDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { mensaje = "Usuario y contraseña requeridos." });
        }

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

        var jwtKey = _configuration["Jwt:Key"] ?? "BarberTurnoClaveSuperSecretaDeSeguridad126!JwtKey2026";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "BarberTurnoApi";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "BarberTurnoApp";
        var expiraEnUtc = DateTime.UtcNow.AddHours(24);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtKey);

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

        return Ok(new LoginResponseDto
        {
            Token = tokenString,
            ExpiraEnUtc = expiraEnUtc,
            Rol = usuario.Rol,
            UsuarioId = usuario.Id,
            Nombre = usuario.Nombre
        });
    }

    [HttpPost("register")]
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

        if (rol == "Barbero")
        {
            var barbero = new BarberTurno.Api.Domain.BarberoEntity
            {
                Nombre = nuevoUsuario.Nombre,
                UsuarioId = nuevoUsuario.Id
            };
            _context.Barberos.Add(barbero);
            await _context.SaveChangesAsync();
        }

        var jwtKey = _configuration["Jwt:Key"] ?? "BarberTurnoClaveSuperSecretaDeSeguridad126!JwtKey2026";
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
            Nombre = nuevoUsuario.Nombre
        });
    }
}
