using System.Text.Json;
using CampusServices.Application.DTOs;

namespace CampusServices.MAUI.Services;

// Keeps the logged-in user's tokens.
// They are saved with SecureStorage (encrypted by the operating system), NOT in browser localStorage.
public class SessionStore
{
    private const string StorageKey = "campus_services_session";

    private AuthResponseDto? _session;
    private bool _loaded;

    // Raised after login, logout or when a refresh gives new tokens.
    public event Action? Changed;

    public async Task<AuthResponseDto?> GetAsync()
    {
        if (_loaded)
        {
            return _session;
        }

        _loaded = true;
        try
        {
            var json = await SecureStorage.Default.GetAsync(StorageKey);
            if (!string.IsNullOrEmpty(json))
            {
                _session = JsonSerializer.Deserialize<AuthResponseDto>(json);
            }
        }
        catch (Exception)
        {
            // SecureStorage is not available or cannot be read on this device.
            // The app still works: the session just lives in memory until the app closes.
            _session = null;
        }

        return _session;
    }

    public async Task SaveAsync(AuthResponseDto session)
    {
        _session = session;
        _loaded = true;

        try
        {
            await SecureStorage.Default.SetAsync(StorageKey, JsonSerializer.Serialize(session));
        }
        catch (Exception)
        {
            // Same as above: keep the session in memory only.
        }

        Changed?.Invoke();
    }

    public void Clear()
    {
        _session = null;
        _loaded = true;

        try
        {
            SecureStorage.Default.Remove(StorageKey);
        }
        catch (Exception)
        {
            // Nothing to remove if SecureStorage is not available.
        }

        Changed?.Invoke();
    }
}
