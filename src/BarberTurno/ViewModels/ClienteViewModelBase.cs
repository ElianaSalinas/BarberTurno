// Responsable: M09 / M01
// Base común para los ViewModels del flujo de cliente con soporte de red y refresco.
using BarberTurno.Services.Network;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BarberTurno.ViewModels;

public abstract partial class ClienteViewModelBase : BaseViewModel
{
    protected readonly INetworkService Network;

    [ObservableProperty]
    private bool isRefreshing;

    public bool HayConexion => Network?.HayConexion ?? true;
    public bool SinConexion => !HayConexion;

    protected ClienteViewModelBase(INetworkService network)
    {
        Network = network;
        if (Network != null)
        {
            Network.ConexionCambio += (s, e) =>
            {
                OnPropertyChanged(nameof(HayConexion));
                OnPropertyChanged(nameof(SinConexion));
            };
        }
    }
}
