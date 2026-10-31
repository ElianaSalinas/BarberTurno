using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BarberTurno.WinUI;

/// <summary>Entrada WinUI que inicia MAUI y registra errores no controlados de Windows.</summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>Inicializa la aplicación WinUI y el registro de errores no controlados.</summary>
	public App()
	{
		this.InitializeComponent();

		UnhandledException += (sender, e) =>
		{
			System.Diagnostics.Debug.WriteLine($"[WinUI UnhandledException] {e.Message}\n{e.Exception}");
			e.Handled = true;
		};
	}

	/// <summary>Obtiene la aplicación MAUI configurada en el proyecto compartido.</summary>
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
