// Responsable: M05
// Resultado tipado de una llamada (evita lanzar excepciones para casos esperados).
namespace BarberTurno.Services.Api;

public class ApiResult<T>
{
    public bool Exito { get; set; }
    public T? Datos { get; set; }
    public int? CodigoHttp { get; set; }   // 409 = hora ocupada
    public bool SinConexion { get; set; }  // fallo de red/timeout: resultado desconocido
    public string? Error { get; set; }
}

