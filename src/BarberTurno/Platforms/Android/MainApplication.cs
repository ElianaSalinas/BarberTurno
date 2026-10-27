using Android.App;
using Android.Runtime;

namespace BarberTurno;

// Responsable: M01. HTTP sin cifrar solo en Debug (API local en pruebas); en Release la app exige HTTPS.
#if DEBUG
[Application(UsesCleartextTraffic = true)]
#else
[Application]
#endif
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
