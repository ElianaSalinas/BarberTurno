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
}
