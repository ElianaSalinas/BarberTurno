using Foundation;

namespace BarberTurno;

/// <summary>Delegado Mac Catalyst que inicia la aplicación MAUI desde el ciclo de vida Apple.</summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	/// <summary>Crea la aplicación usando los servicios registrados de forma compartida.</summary>
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
