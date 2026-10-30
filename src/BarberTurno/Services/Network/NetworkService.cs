// Responsable: M07
// Implementa INetworkService con Connectivity.Current.
using Microsoft.Maui.Networking;

namespace BarberTurno.Services.Network;

/// <summary>Adapta Connectivity de MAUI y notifica a SyncService los cambios de conexión.</summary>
public class NetworkService : INetworkService
{
    /// <summary>Se suscribe al evento de conectividad del dispositivo.</summary>
    public NetworkService()
    {
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    public bool HayConexion => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler<bool>? ConexionCambio;

    private void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        bool conectada = e.NetworkAccess == NetworkAccess.Internet;
        ConexionCambio?.Invoke(this, conectada);
    }
}
