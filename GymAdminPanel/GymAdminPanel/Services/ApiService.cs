using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GymAdminPanel.Models;

namespace GymAdminPanel.Services;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
}

public class ApiService
{
    private readonly HttpClient _httpClient;
    public string Token { get; private set; } = string.Empty;

    public ApiService()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api-j6d6.onrender.com/");
    }
    public async Task<bool> RegisterAsync(string firstName, string lastName, string email, string password)
    {
        var requestData = new
        {
            firstName = firstName,
            lastName = lastName,
            email = email,
            password = password
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", requestData);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var requestData = new { email = email, password = password };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/login", requestData);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null && !string.IsNullOrWhiteSpace(result.Token))
                {
                    Token = result.Token;
                    return true;
                }
            }
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public async Task<List<Client>> GetClientsAsync()
    {
        if (string.IsNullOrEmpty(Token)) return new List<Client>();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.GetAsync("api/admin/users");
            if (response.IsSuccessStatusCode)
            {
                var clients = await response.Content.ReadFromJsonAsync<List<Client>>();
                return clients ?? new List<Client>();
            }
            return new List<Client>();
        }
        catch (System.Exception)
        {
            return new List<Client>();
        }
    }
    public void Logout()
    {
        Token = string.Empty;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }
}