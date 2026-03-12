using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;

namespace Fluxion.Client.Services;

public class FluxionAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationState _anonymous;

    public FluxionAuthStateProvider(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");

        if (string.IsNullOrWhiteSpace(token))
        {
            return ClearAuth();
        }

        try
        {
            var claims = ParseClaimsFromJwt(token).ToList();
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
            
            if (expClaim != null && long.TryParse(expClaim.Value, out var expStr))
            {
                var expDate = DateTimeOffset.FromUnixTimeSeconds(expStr).UtcDateTime;
                if (expDate <= DateTime.UtcNow)
                {
                    // Token is expired. Wipe it out.
                    await _localStorage.RemoveItemAsync("authToken");
                    await _localStorage.RemoveItemAsync("learnerId");
                    return ClearAuth();
                }
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
        }
        catch
        {
            // Corrupt token
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("learnerId");
            return ClearAuth();
        }
    }

    private AuthenticationState ClearAuth()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
        return _anonymous;
    }

    public void NotifyUserAuthentication(string token)
    {
        // Immediately set the auth header so subsequent HTTP calls include the token
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var authenticatedUser = new ClaimsPrincipal(
            new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        NotifyAuthenticationStateChanged(authState);
    }

    public void NotifyUserLogout()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
        var authState = Task.FromResult(_anonymous);
        NotifyAuthenticationStateChanged(authState);
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
        return keyValuePairs!.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!));
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
