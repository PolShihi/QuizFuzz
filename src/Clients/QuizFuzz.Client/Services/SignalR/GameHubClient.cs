using Microsoft.AspNetCore.SignalR.Client;
using QuizFuzz.Client.Services.Auth;

namespace QuizFuzz.Client.Services.SignalR;

public class GameHubClient : IGameHubClient, IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly IAuthService _authService;

    public event Func<Task>? OnConnected;
    public event Func<Task>? OnDisconnected;
    public event Func<string, Task>? OnRoundStarted;
    public event Func<Task>? OnRoundEnded;
    public event Func<string, Task>? OnScoreboardUpdated;

    public GameHubClient(string hubUrl, IAuthService authService)
    {
        _authService = authService;
        
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () => await _authService.GetTokenAsync();
            })
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();
    }

    private void RegisterHandlers()
    {
        _hubConnection.On<string>("RoundStarted", async (data) =>
        {
            if (OnRoundStarted != null)
                await OnRoundStarted.Invoke(data);
        });

        _hubConnection.On("RoundEnded", async () =>
        {
            if (OnRoundEnded != null)
                await OnRoundEnded.Invoke();
        });

        _hubConnection.On<string>("ScoreboardUpdated", async (data) =>
        {
            if (OnScoreboardUpdated != null)
                await OnScoreboardUpdated.Invoke(data);
        });

        _hubConnection.Reconnected += async (connectionId) =>
        {
            if (OnConnected != null)
                await OnConnected.Invoke();
        };

        _hubConnection.Closed += async (error) =>
        {
            if (OnDisconnected != null)
                await OnDisconnected.Invoke();
        };
    }

    public async Task StartAsync()
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            await _hubConnection.StartAsync();
            if (OnConnected != null)
                await OnConnected.Invoke();
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.StopAsync();
        }
    }

    public async Task JoinRoomAsync(Guid roomId)
    {
        await _hubConnection.InvokeAsync("JoinRoom", roomId.ToString());
    }

    public async Task LeaveRoomAsync(Guid roomId)
    {
        await _hubConnection.InvokeAsync("LeaveRoom", roomId.ToString());
    }

    public async Task SubmitAnswerAsync(Guid roundId, string answer)
    {
        await _hubConnection.InvokeAsync("SubmitAnswer", roundId, answer);
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}
