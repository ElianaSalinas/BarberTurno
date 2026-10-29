using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BarberTurno.Dtos;

namespace BarberTurno.Services.Api;

public class BarberTurnoApi : IBarberTurnoApi
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public BarberTurnoApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public void SetAuthToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<ApiResult<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        return await EnviarPeticionAsync<LoginResponseDto>(() =>
            _httpClient.PostAsJsonAsync("auth/login", request, _jsonOptions));
    }

    public async Task<ApiResult<List<BarberoDto>>> GetBarberosAsync()
    {
        return await EnviarPeticionAsync<List<BarberoDto>>(() =>
            _httpClient.GetAsync("barberos"));
    }

    public async Task<ApiResult<List<ServicioDto>>> GetServiciosAsync()
    {
        return await EnviarPeticionAsync<List<ServicioDto>>(() =>
            _httpClient.GetAsync("servicios"));
    }

    public async Task<ApiResult<List<SlotDto>>> GetDisponibilidadAsync(int barberoId, int servicioId, DateOnly fecha)
    {
        var fechaStr = fecha.ToString("yyyy-MM-dd");
        var url = $"disponibilidad?barberoId={barberoId}&servicioId={servicioId}&fecha={fechaStr}";
        return await EnviarPeticionAsync<List<SlotDto>>(() =>
            _httpClient.GetAsync(url));
    }

    public async Task<ApiResult<List<CitaDto>>> GetMisCitasAsync()
    {
        return await EnviarPeticionAsync<List<CitaDto>>(() =>
            _httpClient.GetAsync("citas"));
    }

    public async Task<ApiResult<CitaDto>> CrearCitaAsync(CrearCitaRequestDto request)
    {
        return await EnviarPeticionAsync<CitaDto>(() =>
            _httpClient.PostAsJsonAsync("citas", request, _jsonOptions));
    }

    public async Task<ApiResult<CitaDto>> CancelarCitaAsync(Guid citaId)
    {
        return await EnviarPeticionAsync<CitaDto>(() =>
            _httpClient.PostAsync($"citas/{citaId}/cancelar", null));
    }

    private async Task<ApiResult<T>> EnviarPeticionAsync<T>(Func<Task<HttpResponseMessage>> accionHttp)
    {
        try
        {
            using var response = await accionHttp();
            int statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(content))
                {
                    return new ApiResult<T>
                    {
                        Exito = true,
                        CodigoHttp = statusCode,
                        Datos = default
                    };
                }

                var datos = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                return new ApiResult<T>
                {
                    Exito = true,
                    CodigoHttp = statusCode,
                    Datos = datos
                };
            }

            // Manejo de error retornado por el servidor
            string mensajeError = $"Error en la llamada HTTP ({statusCode}).";
            try
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(errorBody))
                {
                    using var doc = JsonDocument.Parse(errorBody);
                    if (doc.RootElement.TryGetProperty("mensaje", out var propMensaje))
                    {
                        mensajeError = propMensaje.GetString() ?? mensajeError;
                    }
                    else if (doc.RootElement.TryGetProperty("title", out var propTitle))
                    {
                        mensajeError = propTitle.GetString() ?? mensajeError;
                    }
                    else
                    {
                        mensajeError = errorBody;
                    }
                }
            }
            catch
            {
                // Si el cuerpo no era un JSON válido, conservamos el mensaje por defecto
            }

            return new ApiResult<T>
            {
                Exito = false,
                CodigoHttp = statusCode,
                SinConexion = false,
                Error = mensajeError
            };
        }
        catch (TaskCanceledException)
        {
            return new ApiResult<T>
            {
                Exito = false,
                SinConexion = true,
                Error = "Tiempo de espera agotado. Verifique su conexión de red o intente nuevamente."
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>
            {
                Exito = false,
                SinConexion = true,
                Error = "No se pudo establecer conexión con el servidor. Modo sin conexión activado."
            };
        }
        catch (Exception ex)
        {
            return new ApiResult<T>
            {
                Exito = false,
                SinConexion = false,
                Error = $"Error inesperado de red: {ex.Message}"
            };
        }
    }
}
