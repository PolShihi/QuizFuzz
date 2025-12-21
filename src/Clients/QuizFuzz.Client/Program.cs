using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using QuizFuzz.Client;
using QuizFuzz.Client.Services.Auth;
using QuizFuzz.Client.Services.Api;
using Blazored.LocalStorage;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Настройка базового адреса API
var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ?? "https://localhost:7001";

// Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// MudBlazor
builder.Services.AddMudServices();

// Authentication
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
    provider.GetRequiredService<CustomAuthStateProvider>());

// HTTP Clients
builder.Services.AddScoped<AuthenticatedHttpHandler>();

// HttpClient для неавторизованных запросов (auth)
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(apiBaseAddress) 
});

// HttpClient для авторизованных запросов
builder.Services.AddScoped<IRoomsApiClient, RoomsApiClient>(sp =>
{
    var handler = sp.GetRequiredService<AuthenticatedHttpHandler>();
    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseAddress)
    };
    return new RoomsApiClient(httpClient);
});

await builder.Build().RunAsync();
