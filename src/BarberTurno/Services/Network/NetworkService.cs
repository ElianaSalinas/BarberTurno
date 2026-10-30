// Responsable: M07
// Implementa INetworkService con Connectivity.Current.
using Microsoft.Maui.Networking;

namespace BarberTurno.Services.Network;

public class NetworkService : INetworkService
{
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