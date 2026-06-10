namespace QuizFuzz.Web.Api.Services;

public interface IHintRevealScheduler
{
    void ScheduleHints(Guid roomId, Guid roundId);
    void EnsureHintsScheduled(Guid roomId, Guid roundId);
    void StopHints(Guid roundId);
}
