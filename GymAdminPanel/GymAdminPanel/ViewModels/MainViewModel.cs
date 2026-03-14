using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Data;
using GymAdminPanel.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace GymAdminPanel.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Panel Administratora Siłowni";

    [ObservableProperty]
    private ObservableCollection<Client> _clients = new();

    public MainViewModel()
    {
        LoadClients();
    }

    [RelayCommand]
    private void LoadClients()
    {
        using var db = new AppDbContext();
        Clients = new ObservableCollection<Client>(db.Clients.ToList());
    }

    [RelayCommand]
    private void AddTestClient()
    {
        using var db = new AppDbContext();

        var newClient = new Client
        {
            FirstName = "Jan",
            LastName = "Kowalski",
            Email = "jan.kowalski@test.pl"
        };

        db.Clients.Add(newClient);
        db.SaveChanges();

        LoadClients();
    }
}