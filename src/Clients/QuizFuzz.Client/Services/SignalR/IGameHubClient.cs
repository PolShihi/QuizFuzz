namespace QuizFuzz.Client.Services.SignalR;

public interface IGameHubClient
{
    Task StartAsync();
    Task StopAsync();
    Task JoinRoomAsync(Guid roomId);
    Task LeaveRoomAsync(Guid roomId);
    Task SubmitAnswerAsync(Guid roundId, string answer);
    
    event Func<Task>? OnConnected;
    event Func<Task>? OnDisconnected;
    event Func<string, Task>? OnRoundStarted;
    event Func<Task>? OnRoundEnded;
    event Func<string, Task>? OnScoreboardUpdated;
}
