// Responsable: M05
// Resultado tipado del intento de login. Permite al ViewModel mostrar
// mensajes distintos sin depender de códigos HTTP ni excepciones.
namespace BarberTurno.Services.Auth;

/// <summary>Describe el resultado que el ViewModel debe mostrar después de iniciar sesión.</summary>
public enum ResultadoLogin
{
    Exito,                // Token recibido, sesión guardada.
    CredencialesInvalidas, // El servidor respondió 401.
    SinConexion,          // Fallo de red o timeout (resultado desconocido).
    Error                 // Cualquier otra situación inesperada.
}
