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
		AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
		{
			var ex = args.ExceptionObject as Exception;
			var texto = $"[CRASH AppDomain {DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{ex}\n\n";
			Android.Util.Log.Error("BarberTurnoCrash", texto);
			try
			{
				var ruta = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "crash.txt");
				System.IO.File.AppendAllText(ruta, texto);
			}
			catch { }
		};

		Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
		{
			var texto = $"[CRASH AndroidEnvironment {DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{args.Exception}\n\n";
			Android.Util.Log.Error("BarberTurnoCrash", texto);
			try
			{
				var ruta = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "crash.txt");
				System.IO.File.AppendAllText(ruta, texto);
			}
			catch { }
		};
	}

	/// <summary>Conecta el inicio Android con el registro de servicios de <see cref="MauiProgram"/>.</summary>
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
