// Responsable: M5
// Resultado tipado del intento de registro. Permite al ViewModel mostrar
// el mensaje adecuado sin depender de códigos HTTP ni excepciones.
namespace BarberTurno.Services.Auth;

public enum ResultadoRegistro
{
    Exito,             // Cuenta creada y sesión iniciada automáticamente.
    UsuarioYaExiste,   // El servidor respondió 409 (el email/usuario ya está registrado).
    DatosInvalidos,    // El servidor respondió 400 (campos vacíos, formato incorrecto, etc.).
    SinConexion,       // Fallo de red o timeout.
    Error              // Cualquier otra situación inesperada.
}
