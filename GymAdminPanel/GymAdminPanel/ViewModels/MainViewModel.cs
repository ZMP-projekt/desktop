using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Data;
using GymAdminPanel.Models;
using GymAdminPanel.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GymAdminPanel.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private string _title = "Panel Administratora Siłowni";

    [ObservableProperty]
    private ObservableCollection<Client> _clients = new();

    public MainViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _ = LoadClientsAsync();
    }

    [RelayCommand]
    private async Task LoadClientsAsync()
    {
        try
        {
            var clientsFromServer = await _apiService.GetClientsAsync();

            if (clientsFromServer != null && clientsFromServer.Count > 0)
            {
                Clients = new ObservableCollection<Client>(clientsFromServer);

            }
            else
            {
                LoadOfflineData();
            }
        }
        catch
        {
            LoadOfflineData();
        }
    }

    private void LoadOfflineData()
    {
        using var db = new AppDbContext();
        var offlineClients = db.Clients.ToList();
        Clients = new ObservableCollection<Client>(offlineClients);

        Title = "Panel Administratora (TRYB OFFLINE)";
    }
}