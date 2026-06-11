namespace QuizFuzz.Web.Api.Services;

/// <summary>
/// Настройки автоматической очистки комнат.
/// </summary>
public sealed class RoomCleanupOptions
{
    /// <summary>
    /// Включена ли автоматическая очистка комнат.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Как часто фоновый сервис проверяет комнаты.
    /// </summary>
    public int CheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Через сколько минут удалять комнату, которая всё ещё находится в лобби.
    /// </summary>
    public int LobbyLifetimeMinutes { get; set; } = 10;

    /// <summary>
    /// Через сколько секунд удалять пустую комнату без активных игроков.
    /// 0 означает удаление при ближайшей проверке.
    /// </summary>
    public int EmptyRoomGracePeriodSeconds { get; set; } = 0;
}
