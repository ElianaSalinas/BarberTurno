using Android.App;
using Android.Runtime;

namespace BarberTurno;

/// <summary>Aplicación Android que entrega la instancia MAUI configurada por el proyecto compartido.</summary>
[Application]
public class MainApplication : MauiApplication
{
	/// <summary>Construye la aplicación Android a partir del identificador nativo.</summary>
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	/// <summary>Conecta el inicio Android con el registro de servicios de <see cref="MauiProgram"/>.</summary>
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
