namespace BarberTurno.Api.Domain;

public class UsuarioEntity
{
    public int Id { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = "Cliente"; // "Cliente" o "Barbero"
    public string Telefono { get; set; } = string.Empty;
}
