// Responsable: M02
// Único lugar de la app que conoce los nombres exactos de los campos de la API.
// Si la API cambia un campo, SOLO se modifica este archivo.
using BarberTurno.Dtos;
using BarberTurno.Models;

namespace BarberTurno.Mappers;

/// <summary>
/// Traduce entre DTOs (forma del JSON de la API) y entidades locales (forma de SQLite).
/// <para>
/// Reglas de conversión:
/// <list type="bullet">
///   <item>Fechas: el DTO lleva UTC; la entidad almacena UTC y muestra hora local solo en la UI.</item>
///   <item>Estado: viaja como string en minúsculas en el JSON; aquí se convierte a/desde <see cref="EstadoCita"/>.</item>
///   <item>Valores nulos o desconocidos: se usa un valor seguro por defecto (nunca lanza excepción).</item>
/// </list>
/// </para>
/// </summary>
public static class DtoMapper
{
    // ── Barbero ───────────────────────────────────────────────────────────────

    /// <summary>Convierte un BarberoDto (respuesta de la API) en entidad local.</summary>
    public static Barbero ToEntidad(this BarberoDto dto) => new()
    {
        Id      = dto.Id,
        Nombre  = dto.Nombre ?? "",
        FotoUrl = dto.FotoUrl
    };

    /// <summary>Convierte una entidad Barbero en DTO para enviar a la API.</summary>
    public static BarberoDto ToDto(this Barbero entidad) => new()
    {
        Id          = entidad.Id,
        Nombre      = entidad.Nombre,
        FotoUrl     = entidad.FotoUrl,
        ServicioIds = new List<int>() // se rellena en capa de servicio si se necesita
    };

    // ── Servicio ──────────────────────────────────────────────────────────────

    /// <summary>Convierte un ServicioDto (respuesta de la API) en entidad local.</summary>
    public static Servicio ToEntidad(this ServicioDto dto) => new()
    {
        Id          = dto.Id,
        Nombre      = dto.Nombre ?? "",
        DuracionMin = dto.DuracionMin,
        Precio      = dto.Precio
    };

    /// <summary>Convierte una entidad Servicio en DTO para enviar a la API.</summary>
    public static ServicioDto ToDto(this Servicio entidad) => new()
    {
        Id          = entidad.Id,
        Nombre      = entidad.Nombre,
        DuracionMin = entidad.DuracionMin,
        Precio      = entidad.Precio
    };

    // ── Cita ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Convierte un CitaDto (respuesta de la API) en entidad local.
    /// Las fechas UTC del DTO se convierten a hora local para la entidad.
    /// El estado string se convierte a <see cref="EstadoCita"/>.
    /// </summary>
    public static Cita ToEntidad(this CitaDto dto) => new()
    {
        Id                   = dto.Id,
        ClienteId            = dto.ClienteId,
        BarberoId            = dto.BarberoId,
        ServicioId           = dto.ServicioId,
        Inicio               = DateTime.SpecifyKind(dto.InicioUtc, DateTimeKind.Utc).ToLocalTime(),
        Fin                  = DateTime.SpecifyKind(dto.FinUtc,    DateTimeKind.Utc).ToLocalTime(),
        Estado               = ParsearEstado(dto.Estado),
        MotivoRechazo        = dto.MotivoRechazo,
        PendienteSincronizar = false
    };

    /// <summary>
    /// Convierte una entidad Cita en CitaDto para enviar a la API.
    /// Las fechas locales de la entidad se convierten a UTC para el DTO.
    /// El enum <see cref="EstadoCita"/> se convierte al string esperado por la API.
    /// </summary>
    public static CitaDto ToDto(this Cita entidad) => new()
    {
        Id            = entidad.Id,
        ClienteId     = entidad.ClienteId,
        BarberoId     = entidad.BarberoId,
        ServicioId    = entidad.ServicioId,
        InicioUtc     = entidad.Inicio.Kind == DateTimeKind.Utc
                            ? entidad.Inicio
                            : entidad.Inicio.ToUniversalTime(),
        FinUtc        = entidad.Fin.Kind == DateTimeKind.Utc
                            ? entidad.Fin
                            : entidad.Fin.ToUniversalTime(),
        Estado        = entidad.Estado.ToTexto(),
        MotivoRechazo = entidad.MotivoRechazo
    };

    // ── SlotDisponible ────────────────────────────────────────────────────────

    /// <summary>
    /// Convierte un SlotDto (franja de disponibilidad de la API) en objeto de dominio.
    /// Las fechas UTC se convierten a hora local para mostrarlas al cliente.
    /// </summary>
    public static SlotDisponible ToEntidad(this SlotDto dto) => new()
    {
        Inicio = DateTime.SpecifyKind(dto.InicioUtc, DateTimeKind.Utc).ToLocalTime(),
        Fin    = DateTime.SpecifyKind(dto.FinUtc,    DateTimeKind.Utc).ToLocalTime()
    };

    // ── SesionUsuario ─────────────────────────────────────────────────────────

    /// <summary>
    /// Construye la sesión en memoria a partir de la respuesta de login de la API.
    /// El token se almacena en SecureStorage por M05; aquí queda en la sesión para
    /// adjuntarlo a las peticiones HTTP.
    /// </summary>
    public static SesionUsuario ToSesion(this LoginResponseDto dto) => new()
    {
        UsuarioId = dto.UsuarioId,
        Nombre    = dto.Nombre ?? "",
        Rol       = dto.Rol?.Equals("Barbero", StringComparison.OrdinalIgnoreCase) == true
                        ? RolUsuario.Barbero
                        : RolUsuario.Cliente,
        Token     = dto.Token ?? ""
    };

    // ── CrearCitaRequestDto ───────────────────────────────────────────────────

    /// <summary>
    /// Construye el cuerpo de la petición de creación de cita para la API.
    /// Usa el Id de la entidad como clave de idempotencia.
    /// </summary>
    public static CrearCitaRequestDto ToCrearRequest(this Cita entidad) => new()
    {
        Id         = entidad.Id,
        BarberoId  = entidad.BarberoId,
        ServicioId = entidad.ServicioId,
        InicioUtc  = entidad.Inicio.Kind == DateTimeKind.Utc
                         ? entidad.Inicio
                         : entidad.Inicio.ToUniversalTime()
    };

    // ── Helpers privados ──────────────────────────────────────────────────────

    /// <summary>
    /// Convierte el string de estado que devuelve la API al enum local.
    /// Si el valor es nulo o desconocido, devuelve <see cref="EstadoCita.Pendiente"/> como valor seguro.
    /// </summary>
    private static EstadoCita ParsearEstado(string? texto) =>
        texto?.ToLowerInvariant() switch
        {
            "pendiente"   => EstadoCita.Pendiente,
            "confirmada"  => EstadoCita.Confirmada,
            "rechazada"   => EstadoCita.Rechazada,
            "cancelada"   => EstadoCita.Cancelada,
            "atendida"    => EstadoCita.Atendida,
            "ausente"     => EstadoCita.Ausente,
            _             => EstadoCita.Pendiente   // valor seguro ante valores desconocidos
        };

    /// <summary>
    /// Convierte el enum local al string en minúsculas que espera la API.
    /// </summary>
    private static string ToTexto(this EstadoCita estado) =>
        estado switch
        {
            EstadoCita.Pendiente   => "pendiente",
            EstadoCita.Confirmada  => "confirmada",
            EstadoCita.Rechazada   => "rechazada",
            EstadoCita.Cancelada   => "cancelada",
            EstadoCita.Atendida    => "atendida",
            EstadoCita.Ausente     => "ausente",
            _                     => "pendiente"
        };
}
