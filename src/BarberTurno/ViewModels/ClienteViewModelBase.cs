// Responsable: M09 / M01
// Base común para los ViewModels del flujo de cliente con soporte de red seguro sin fuga de memoria.
using BarberTurno.Services.Network;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.ApplicationModel;

namespace BarberTurno.ViewModels;

/// <summary>Base de las pantallas de cliente que refleja los cambios de conectividad.</summary>
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
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        if (Network != null)
        {
            Network.ConexionCambio -= OnConexionCambio;
            Network.ConexionCambio += OnConexionCambio;
        }
    }

    public override void OnDisappearing()
    {
        base.OnDisappearing();
        if (Network != null)
        {
            Network.ConexionCambio -= OnConexionCambio;
        }
    }

    private void OnConexionCambio(object? sender, bool e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(HayConexion));
            OnPropertyChanged(nameof(SinConexion));
        });
    }
}
