using System.ComponentModel.DataAnnotations;

namespace QuizFuzz.Shared.Dtos.Game;

public class SubmitAnswerRequest
{
    [Required]
    public Guid RoundId { get; set; }
    
    [Required]
    [MinLength(1)]
    public string AnswerText { get; set; } = string.Empty;
    
    public int AnswerTimeMs { get; set; }
}
