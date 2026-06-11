using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using QuizFuzz.Client;
using QuizFuzz.Client.Services.Auth;
using QuizFuzz.Client.Services;
using QuizFuzz.Client.Services.Api;
using Blazored.LocalStorage;
using MudBlazor.Services;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

#if DEBUG
builder.Logging.SetMinimumLevel(LogLevel.Debug);
#else
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.None);
#endif

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Настройка базового адреса API
var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ?? "https://localhost:7001";

// Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Локализация
builder.Services.AddLocalization();

// MudBlazor
builder.Services.AddMudServices();
builder.Services.AddScoped<AppThemeService>();

// Authentication
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
    provider.GetRequiredService<CustomAuthStateProvider>());

// HTTP Clients
// HttpClient для неавторизованных запросов (auth)
builder.Services.AddScoped(sp => 
{
    var httpClient = new HttpClient 
    { 
        BaseAddress = new Uri(apiBaseAddress) 
    };
    return httpClient;
});

// AuthenticatedHttpHandler (использует IAuthService, не HttpClient напрямую)
builder.Services.AddTransient<AuthenticatedHttpHandler>();

// HttpClient для авторизованных запросов
builder.Services.AddScoped<IRoomsApiClient>(sp =>
{
    var authService = sp.GetRequiredService<IAuthService>();
    var handler = new AuthenticatedHttpHandler(authService, new HttpClientHandler());
    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseAddress)
    };
    return new RoomsApiClient(httpClient);
});

builder.Services.AddScoped<IGameApiClient>(sp =>
{
    var authService = sp.GetRequiredService<IAuthService>();
    var handler = new AuthenticatedHttpHandler(authService, new HttpClientHandler());
    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseAddress)
    };
    return new GameApiClient(httpClient);
});

builder.Services.AddScoped<IUsersApiClient>(sp =>
{
    var authService = sp.GetRequiredService<IAuthService>();
    var handler = new AuthenticatedHttpHandler(authService, new HttpClientHandler());
    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseAddress)
    };
    return new UsersApiClient(httpClient);
});

builder.Services.AddScoped<IQuestionsApiClient>(sp =>
{
    var authService = sp.GetRequiredService<IAuthService>();
    var handler = new AuthenticatedHttpHandler(authService, new HttpClientHandler());
    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseAddress)
    };
    return new QuestionsApiClient(httpClient);
});

builder.Services.AddScoped<IModerationApiClient>(sp =>
{
    var authService = sp.GetRequiredService<IAuthService>();
    var handler = new AuthenticatedHttpHandler(authService, new HttpClientHandler());
    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseAddress)
    };
    return new ModerationApiClient(httpClient);
});

builder.Services.AddScoped<IAdminApiClient>(sp =>
{
    var authService = sp.GetRequiredService<IAuthService>();
    var handler = new AuthenticatedHttpHandler(authService, new HttpClientHandler());
    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseAddress)
    };
    return new AdminApiClient(httpClient);
});

builder.Services.AddScoped<ITagsApiClient>(sp =>
{
    var authService = sp.GetRequiredService<IAuthService>();
    var handler = new AuthenticatedHttpHandler(authService, new HttpClientHandler());
    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseAddress)
    };
    return new TagsApiClient(httpClient, sp.GetRequiredService<ILogger<TagsApiClient>>());
});

builder.Services.AddScoped<IMediaApiClient>(sp =>
{
    var authService = sp.GetRequiredService<IAuthService>();
    var handler = new AuthenticatedHttpHandler(authService, new HttpClientHandler());
    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseAddress)
    };
    return new MediaApiClient(httpClient, sp.GetRequiredService<ILogger<MediaApiClient>>());
});
builder.Services.AddScoped<ISubscriptionsApiClient>(sp =>
{
    var authService = sp.GetRequiredService<IAuthService>();
    var handler = new AuthenticatedHttpHandler(authService, new HttpClientHandler());
    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseAddress)
    };
    return new SubscriptionsApiClient(httpClient);
});

builder.Services.AddScoped<ILeaderboardApiClient>(sp =>
{
    var authService = sp.GetRequiredService<IAuthService>();
    var handler = new AuthenticatedHttpHandler(authService, new HttpClientHandler());
    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseAddress)
    };
    return new LeaderboardApiClient(httpClient);
});


var host = builder.Build();

var jsRuntime = host.Services.GetRequiredService<IJSRuntime>();
var cultureName = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", "QuizFuzz.Culture") ?? "ru-RU";

if (cultureName is not ("ru-RU" or "en-US"))
{
    cultureName = "ru-RU";
}

var culture = new CultureInfo(cultureName);
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;
CultureInfo.CurrentCulture = culture;
CultureInfo.CurrentUICulture = culture;

await host.RunAsync();
