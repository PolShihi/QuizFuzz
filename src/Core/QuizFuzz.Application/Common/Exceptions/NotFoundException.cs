namespace QuizFuzz.Application.Common.Exceptions;

/// <summary>
/// Исключение "не найдено"
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.")
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }
}
