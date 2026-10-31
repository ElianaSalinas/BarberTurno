using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BarberTurno.Dtos;

namespace BarberTurno.Services.Api;

/// <summary>Cliente HTTP que implementa <see cref="IBarberTurnoApi"/> y traduce respuestas REST a <see cref="ApiResult{T}"/>.</summary>
public class BarberTurnoApi : IBarberTurnoApi
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>Recibe el cliente configurado por DI con la dirección base de la API.</summary>
    public BarberTurnoApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>Adjunta el JWT a las solicitudes posteriores o lo limpia al cerrar sesión.</summary>
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

    /// <summary>Envía las credenciales al endpoint de inicio de sesión.</summary>
    public async Task<ApiResult<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        return await EnviarPeticionAsync<LoginResponseDto>(() =>
            _httpClient.PostAsJsonAsync("auth/login", request, _jsonOptions));
    }

    /// <summary>Registra un usuario mediante la API y devuelve su sesión.</summary>
    public async Task<ApiResult<LoginResponseDto>> RegistrarAsync(RegistroRequestDto request)
    {
        return await EnviarPeticionAsync<LoginResponseDto>(() =>
            _httpClient.PostAsJsonAsync("auth/register", request, _jsonOptions));
    }

    public async Task<ApiResult<List<ClienteDto>>> GetClientesAsync()
    {
        return await EnviarPeticionAsync<List<ClienteDto>>(() =>
            _httpClient.GetAsync("clientes"));
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

        public async Task<ApiResult<bool>> ActualizarFotoBarberoAsync(int barberoId, string fotoUrl)
    {
        return await EnviarPeticionAsync<bool>(() =>
            _httpClient.PutAsJsonAsync($"barberos/{barberoId}/foto", new ActualizarFotoDto { FotoUrl = fotoUrl }, _jsonOptions));
    }

    public async Task<ApiResult<bool>> ActualizarServiciosBarberoAsync(int barberoId, List<int> servicioIds)
    {
        return await EnviarPeticionAsync<bool>(() =>
            _httpClient.PutAsJsonAsync($"barberos/{barberoId}/servicios", servicioIds, _jsonOptions));
    }

    public async Task<ApiResult<ServicioDto>> CrearServicioAsync(CrearServicioDto request)
    {
        return await EnviarPeticionAsync<ServicioDto>(() =>
            _httpClient.PostAsJsonAsync("servicios", request, _jsonOptions));
    }

    /// <summary>Ejecuta una solicitud, deserializa su respuesta y normaliza errores de red y HTTP.</summary>
    private async Task<ApiResult<T>> EnviarPeticionAsync<T>(Func<Task<HttpResponseMessage>> accionHttp)
    {
        try
        {
            // Todas las operaciones públicas pasan por aquí para devolver un resultado uniforme.
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

            return new ApiResult<T>
            {
                Exito = false,
                CodigoHttp = statusCode
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>
            {
                Exito = false,
                SinConexion = true
            };
        }
        catch (TaskCanceledException)
        {
            return new ApiResult<T>
            {
                Exito = false,
                SinConexion = true
            };
        }
        catch (Exception)
        {
            return new ApiResult<T>
            {
                Exito = false
            };
        }
    }
}


