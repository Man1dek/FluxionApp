using Microsoft.JSInterop;
using Blazored.LocalStorage;

namespace Fluxion.Client.Services
{
    public class AnimationService
    {
        private readonly ILocalStorageService _localStorage;
        private readonly IJSRuntime _js;

        public bool AreAnimationsEnabled { get; private set; } = true;
        public event Action? OnAnimationToggled;

        public AnimationService(ILocalStorageService localStorage, IJSRuntime js)
        {
            _localStorage = localStorage;
            _js = js;
        }

        public async Task InitializeAsync()
        {
            var stored = await _localStorage.GetItemAsync<string>("fluxion_animations");
            // Default to 'on' if not set
            AreAnimationsEnabled = stored != "off";
            OnAnimationToggled?.Invoke();
        }

        public async Task ToggleAnimationsAsync()
        {
            AreAnimationsEnabled = !AreAnimationsEnabled;
            await _localStorage.SetItemAsync("fluxion_animations", AreAnimationsEnabled ? "on" : "off");
            OnAnimationToggled?.Invoke();
        }
    }
}
