using BarberTurno.Dtos;

namespace BarberTurno.Services.Api;

/// <summary>Define las llamadas REST que conectan la aplicación móvil con la API de BarberTurno.</summary>
public interface IBarberTurnoApi
{
    /// <summary>Configura o elimina el token JWT que acompaña las solicitudes autenticadas.</summary>
    void SetAuthToken(string? token);
    Task<ApiResult<LoginResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ApiResult<LoginResponseDto>> RegistrarAsync(RegistroRequestDto request);
    Task<ApiResult<List<BarberoDto>>> GetBarberosAsync();
    Task<ApiResult<List<ClienteDto>>> GetClientesAsync();
    Task<ApiResult<List<ServicioDto>>> GetServiciosAsync();
    Task<ApiResult<List<SlotDto>>> GetDisponibilidadAsync(int barberoId, int servicioId, DateOnly fecha);
    Task<ApiResult<List<CitaDto>>> GetMisCitasAsync();
    Task<ApiResult<CitaDto>> CrearCitaAsync(CrearCitaRequestDto request);   // 409 si la hora está ocupada
    Task<ApiResult<CitaDto>> CancelarCitaAsync(Guid citaId);
    Task<ApiResult<bool>> ActualizarFotoBarberoAsync(int barberoId, string fotoUrl);
    Task<ApiResult<bool>> ActualizarServiciosBarberoAsync(int barberoId, List<int> servicioIds);
    Task<ApiResult<ServicioDto>> CrearServicioAsync(CrearServicioDto request);
}

