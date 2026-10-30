namespace BarberTurno.Helpers;

// Responsable: M01 / M05 / M06
// Constantes globales de configuración.
// NOTA: Para desarrollo local en Android (teléfono físico o emulador), se utiliza el reenvío
// de puertos con 'adb reverse tcp:5000 tcp:5000' para comunicar localhost:5000 con la PC.
// Para el entorno de producción final, la URL debe obtenerse de configuración y utilizar HTTPS (ADR-0012).
public static class AppConstants
{
    public static string ApiBaseUrl
    {
        get
        {
#if ANDROID
            // Vía adb reverse tcp:5000 tcp:5000 (funciona tanto en teléfono físico por USB como en emulador)
            return "http://127.0.0.1:5000/api/";
#else
            return "http://localhost:5000/api/";
#endif
        }
    }
}
