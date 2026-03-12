using Microsoft.JSInterop;
using Blazored.LocalStorage;

namespace Fluxion.Client.Services;

public class ThemeService
{
    private readonly ILocalStorageService _localStorage;
    public bool IsDarkMode { get; private set; } = true;
    public event Action? OnThemeChanged;

    public ThemeService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task InitializeAsync(IJSRuntime js)
    {
        try
        {
            var saved = await _localStorage.GetItemAsync<string>("fluxion_theme");
            IsDarkMode = saved != "light";
            await js.InvokeVoidAsync("fluxionTheme.apply", IsDarkMode);
        }
        catch 
        { 
            IsDarkMode = true; 
        }
        OnThemeChanged?.Invoke();
    }

    public async Task ToggleAsync(IJSRuntime js)
    {
        IsDarkMode = !IsDarkMode;
        try
        {
            await _localStorage.SetItemAsync("fluxion_theme", IsDarkMode ? "dark" : "light");
            await js.InvokeVoidAsync("fluxionTheme.apply", IsDarkMode);
        }
        catch { }
        OnThemeChanged?.Invoke();
    }
}
