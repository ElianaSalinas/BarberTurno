using BarberTurno.Dtos;

namespace BarberTurno.Services.Api;

public interface IBarberTurnoApi
{
    void SetAuthToken(string? token);
    Task<ApiResult<LoginResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ApiResult<List<BarberoDto>>> GetBarberosAsync();
    Task<ApiResult<List<ServicioDto>>> GetServiciosAsync();
    Task<ApiResult<List<SlotDto>>> GetDisponibilidadAsync(int barberoId, int servicioId, DateOnly fecha);
    Task<ApiResult<List<CitaDto>>> GetMisCitasAsync();
    Task<ApiResult<CitaDto>> CrearCitaAsync(CrearCitaRequestDto request);   // 409 si la hora está ocupada
    Task<ApiResult<CitaDto>> CancelarCitaAsync(Guid citaId);
}
