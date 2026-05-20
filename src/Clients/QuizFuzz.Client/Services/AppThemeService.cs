using Blazored.LocalStorage;

namespace QuizFuzz.Client.Services;

public sealed class AppThemeService
{
    private const string StorageKey = "quizfuzz.theme.isDarkMode";
    private readonly ILocalStorageService _localStorage;
    private bool _isInitialized;

    public AppThemeService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public bool IsDarkMode { get; private set; } = true;

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        try
        {
            if (await _localStorage.ContainKeyAsync(StorageKey))
            {
                IsDarkMode = await _localStorage.GetItemAsync<bool>(StorageKey);
            }
        }
        catch
        {
            IsDarkMode = true;
        }

        NotifyChanged();
    }

    public async Task ToggleAsync()
    {
        IsDarkMode = !IsDarkMode;

        try
        {
            await _localStorage.SetItemAsync(StorageKey, IsDarkMode);
        }
        finally
        {
            NotifyChanged();
        }
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
