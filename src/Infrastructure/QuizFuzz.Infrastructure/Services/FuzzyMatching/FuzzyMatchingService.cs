using Microsoft.Extensions.Logging;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Infrastructure.Services.FuzzyMatching;

/// <summary>
/// Сервис нечеткого сопоставления ответов
/// </summary>
public class FuzzyMatchingService : IFuzzyMatchingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FuzzyMatchingService> _logger;

    // Thresholds для разных стратегий
    private const decimal ExactMatchThreshold = 1.0m;
    private const decimal HighConfidenceThreshold = 0.9m;
    private const decimal MediumConfidenceThreshold = 0.75m;
    private const decimal LowConfidenceThreshold = 0.6m;
    private const int MaxEditDistance = 2;

    public FuzzyMatchingService(
        IUnitOfWork unitOfWork,
        ILogger<FuzzyMatchingService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MatchResult> EvaluateAnswerAsync(
        Guid questionId,
        string userAnswer,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userAnswer))
        {
            return new MatchResult
            {
                IsCorrect = false,
                Strategy = MatchStrategy.Rejected,
                Confidence = 0m,
                NormalizedAnswer = string.Empty
            };
        }

        // Получаем вопрос с ответами и алиасами
        var question = await _unitOfWork.Questions.GetWithAllDetailsAsync(questionId, cancellationToken);
        if (question == null)
        {
            _logger.LogWarning("Question {QuestionId} not found", questionId);
            throw new InvalidOperationException($"Question {questionId} not found");
        }

        var normalizedUserAnswer = TextNormalizer.Normalize(userAnswer);

        // 1. Exact match (нормализованный)
        var exactMatch = await TryExactMatchAsync(question.Answers, normalizedUserAnswer);
        if (exactMatch != null)
            return exactMatch;

        // 2. Alias match
        var aliasMatch = await TryAliasMatchAsync(question.Answers, normalizedUserAnswer);
        if (aliasMatch != null)
            return aliasMatch;

        // 3. Edit distance (Levenshtein)
        var editDistanceMatch = TryEditDistanceMatch(question.Answers, normalizedUserAnswer);
        if (editDistanceMatch != null)
            return editDistanceMatch;

        // 4. Token similarity
        var tokenMatch = TryTokenMatch(question.Answers, normalizedUserAnswer);
        if (tokenMatch != null)
            return tokenMatch;

        // 5. No match
        return new MatchResult
        {
            IsCorrect = false,
            Strategy = MatchStrategy.Rejected,
            Confidence = 0m,
            NormalizedAnswer = normalizedUserAnswer
        };
    }

    private async Task<MatchResult?> TryExactMatchAsync(
        IReadOnlyCollection<Domain.Entities.QuestionAnswer> answers,
        string normalizedUserAnswer)
    {
        foreach (var answer in answers.Where(a => a.IsActive))
        {
            if (answer.NormalizedAnswer == normalizedUserAnswer)
            {
                _logger.LogInformation(
                    "Exact match found for answer {AnswerId}",
                    answer.Id);

                return new MatchResult
                {
                    IsCorrect = true,
                    Strategy = MatchStrategy.Exact,
                    Confidence = ExactMatchThreshold,
                    NormalizedAnswer = normalizedUserAnswer,
                    MatchedQuestionAnswerId = answer.Id
                };
            }
        }

        return null;
    }

    private async Task<MatchResult?> TryAliasMatchAsync(
        IReadOnlyCollection<Domain.Entities.QuestionAnswer> answers,
        string normalizedUserAnswer)
    {
        foreach (var answer in answers.Where(a => a.IsActive))
        {
            foreach (var alias in answer.Aliases)
            {
                if (alias.NormalizedAlias == normalizedUserAnswer)
                {
                    _logger.LogInformation(
                        "Alias match found: {AliasId} for answer {AnswerId}",
                        alias.Id, answer.Id);

                    return new MatchResult
                    {
                        IsCorrect = true,
                        Strategy = MatchStrategy.Alias,
                        Confidence = HighConfidenceThreshold,
                        NormalizedAnswer = normalizedUserAnswer,
                        MatchedQuestionAnswerId = answer.Id,
                        MatchedAliasId = alias.Id
                    };
                }
            }
        }

        return null;
    }

    private MatchResult? TryEditDistanceMatch(
        IReadOnlyCollection<Domain.Entities.QuestionAnswer> answers,
        string normalizedUserAnswer)
    {
        MatchResult? bestMatch = null;
        var bestSimilarity = 0m;

        foreach (var answer in answers.Where(a => a.IsActive))
        {
            var similarity = LevenshteinDistance.CalculateSimilarity(
                answer.NormalizedAnswer,
                normalizedUserAnswer);

            if (similarity >= HighConfidenceThreshold && similarity > bestSimilarity)
            {
                var distance = LevenshteinDistance.Calculate(
                    answer.NormalizedAnswer,
                    normalizedUserAnswer);

                if (distance <= MaxEditDistance)
                {
                    bestSimilarity = similarity;
                    bestMatch = new MatchResult
                    {
                        IsCorrect = true,
                        Strategy = MatchStrategy.EditDistance,
                        Confidence = similarity,
                        NormalizedAnswer = normalizedUserAnswer,
                        MatchedQuestionAnswerId = answer.Id
                    };

                    _logger.LogInformation(
                        "Edit distance match: {Similarity:P2}, distance: {Distance} for answer {AnswerId}",
                        similarity, distance, answer.Id);
                }
            }
        }

        return bestMatch;
    }

    private MatchResult? TryTokenMatch(
        IReadOnlyCollection<Domain.Entities.QuestionAnswer> answers,
        string normalizedUserAnswer)
    {
        MatchResult? bestMatch = null;
        var bestSimilarity = 0m;

        foreach (var answer in answers.Where(a => a.IsActive))
        {
            var weightedRatio = TokenSimilarity.CalculateWeightedRatio(
                answer.NormalizedAnswer,
                normalizedUserAnswer);

            if (weightedRatio >= MediumConfidenceThreshold && weightedRatio > bestSimilarity)
            {
                bestSimilarity = weightedRatio;
                bestMatch = new MatchResult
                {
                    IsCorrect = true,
                    Strategy = MatchStrategy.Token,
                    Confidence = weightedRatio,
                    NormalizedAnswer = normalizedUserAnswer,
                    MatchedQuestionAnswerId = answer.Id
                };

                _logger.LogInformation(
                    "Token match: {Similarity:P2} for answer {AnswerId}",
                    weightedRatio, answer.Id);
            }
        }

        return bestMatch;
    }
}
