using Microsoft.JSInterop;
using Blazored.LocalStorage;

namespace Fluxion.Client.Services
{
    public class SoundService
    {
        private readonly ILocalStorageService _localStorage;
        private readonly IJSRuntime _js;

        public bool IsSoundEnabled { get; private set; } = false;
        public event Action? OnSoundToggled;

        public SoundService(ILocalStorageService localStorage, IJSRuntime js)
        {
            _localStorage = localStorage;
            _js = js;
        }

        public async Task InitializeAsync()
        {
            var storedSound = await _localStorage.GetItemAsync<string>("fluxion_sound");
            IsSoundEnabled = storedSound == "on";
            try { await _js.InvokeVoidAsync("fluxionSound.init", IsSoundEnabled); } catch { }
            OnSoundToggled?.Invoke();
        }

        public async Task ToggleSoundAsync()
        {
            IsSoundEnabled = !IsSoundEnabled;
            await _localStorage.SetItemAsync("fluxion_sound", IsSoundEnabled ? "on" : "off");
            try { await _js.InvokeVoidAsync("fluxionSound.setEnabled", IsSoundEnabled); } catch { }
            OnSoundToggled?.Invoke();
        }

        public async Task PlayCorrectAsync() => await PlayAsync("correct");
        public async Task PlayWrongAsync() => await PlayAsync("wrong");
        public async Task PlayStepAdvanceAsync() => await PlayAsync("stepAdvance");
        public async Task PlayMasteredAsync() => await PlayAsync("mastered");
        public async Task PlayXpAsync() => await PlayAsync("xp");

        private async Task PlayAsync(string methodName)
        {
            if (IsSoundEnabled)
            {
                try
                {
                    await _js.InvokeVoidAsync($"fluxionSound.{methodName}");
                }
                catch { /* Ignore JS errors */ }
            }
        }
    }
}
