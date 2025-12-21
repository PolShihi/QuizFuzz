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
        _logger.LogInformation("🔍 [FuzzyMatching] ========== EVALUATING ANSWER ==========");
        _logger.LogInformation("🔍 [FuzzyMatching] Question ID: {QuestionId}", questionId);
        _logger.LogInformation("🔍 [FuzzyMatching] User Answer: '{UserAnswer}'", userAnswer);
        
        if (string.IsNullOrWhiteSpace(userAnswer))
        {
            _logger.LogWarning("❌ [FuzzyMatching] Answer is empty!");
            return new MatchResult
            {
                IsCorrect = false,
                Strategy = MatchStrategy.Rejected,
                Confidence = 0m,
                NormalizedAnswer = string.Empty
            };
        }

        // Получаем вопрос с ответами и алиасами
        _logger.LogInformation("📥 [FuzzyMatching] Loading question with answers and aliases...");
        var question = await _unitOfWork.Questions.GetWithAllDetailsAsync(questionId, cancellationToken);
        if (question == null)
        {
            _logger.LogError("❌ [FuzzyMatching] Question {QuestionId} not found!", questionId);
            throw new InvalidOperationException($"Question {questionId} not found");
        }

        _logger.LogInformation("✅ [FuzzyMatching] Question loaded: '{QuestionText}'", question.PromptText);
        _logger.LogInformation("📊 [FuzzyMatching] Question has {Count} answers", question.Answers.Count);
        
        foreach (var ans in question.Answers)
        {
            _logger.LogInformation("   Answer {Id}: '{Text}' (IsPrimary: {IsPrimary}, IsActive: {IsActive}, Aliases: {AliasCount})", 
                ans.Id, ans.AnswerText, ans.IsPrimary, ans.IsActive, ans.Aliases.Count);
            
            foreach (var alias in ans.Aliases)
            {
                _logger.LogInformation("      Alias: '{AliasText}' → '{NormalizedAlias}'", 
                    alias.AliasText, alias.NormalizedAlias);
            }
        }

        var normalizedUserAnswer = TextNormalizer.Normalize(userAnswer);
        _logger.LogInformation("🔄 [FuzzyMatching] Normalized user answer: '{Normalized}'", normalizedUserAnswer);

        // 1. Exact match (нормализованный)
        _logger.LogInformation("🔍 [FuzzyMatching] Step 1: Trying EXACT match...");
        var exactMatch = await TryExactMatchAsync(question.Answers, normalizedUserAnswer);
        if (exactMatch != null)
        {
            _logger.LogInformation("✅ [FuzzyMatching] EXACT MATCH FOUND!");
            return exactMatch;
        }
        _logger.LogInformation("⏭️ [FuzzyMatching] No exact match");

        // 2. Alias match
        _logger.LogInformation("🔍 [FuzzyMatching] Step 2: Trying ALIAS match...");
        var aliasMatch = await TryAliasMatchAsync(question.Answers, normalizedUserAnswer);
        if (aliasMatch != null)
        {
            _logger.LogInformation("✅ [FuzzyMatching] ALIAS MATCH FOUND!");
            return aliasMatch;
        }
        _logger.LogInformation("⏭️ [FuzzyMatching] No alias match");

        // 3. Edit distance (Levenshtein)
        _logger.LogInformation("🔍 [FuzzyMatching] Step 3: Trying EDIT DISTANCE match...");
        var editDistanceMatch = TryEditDistanceMatch(question.Answers, normalizedUserAnswer);
        if (editDistanceMatch != null)
        {
            _logger.LogInformation("✅ [FuzzyMatching] EDIT DISTANCE MATCH FOUND!");
            return editDistanceMatch;
        }
        _logger.LogInformation("⏭️ [FuzzyMatching] No edit distance match");

        // 4. Token similarity
        _logger.LogInformation("🔍 [FuzzyMatching] Step 4: Trying TOKEN match...");
        var tokenMatch = TryTokenMatch(question.Answers, normalizedUserAnswer);
        if (tokenMatch != null)
        {
            _logger.LogInformation("✅ [FuzzyMatching] TOKEN MATCH FOUND!");
            return tokenMatch;
        }
        _logger.LogInformation("⏭️ [FuzzyMatching] No token match");

        // 5. No match
        _logger.LogWarning("❌ [FuzzyMatching] NO MATCH FOUND - Answer is INCORRECT");
        _logger.LogInformation("🔍 [FuzzyMatching] ========== EVALUATION COMPLETE: INCORRECT ==========");
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
        _logger.LogInformation("   Checking {Count} active answers for exact match...", answers.Count(a => a.IsActive));
        
        foreach (var answer in answers.Where(a => a.IsActive))
        {
            _logger.LogInformation("   Comparing: '{Correct}' == '{User}' ?", 
                answer.NormalizedAnswer, normalizedUserAnswer);
            
            if (answer.NormalizedAnswer == normalizedUserAnswer)
            {
                _logger.LogInformation("   ✅ EXACT MATCH! Answer ID: {AnswerId}", answer.Id);

                return new MatchResult
                {
                    IsCorrect = true,
                    Strategy = MatchStrategy.Exact,
                    Confidence = ExactMatchThreshold,
                    NormalizedAnswer = normalizedUserAnswer,
                    MatchedQuestionAnswerId = answer.Id
                };
            }
            else
            {
                _logger.LogInformation("   ❌ Not equal");
            }
        }

        return null;
    }

    private async Task<MatchResult?> TryAliasMatchAsync(
        IReadOnlyCollection<Domain.Entities.QuestionAnswer> answers,
        string normalizedUserAnswer)
    {
        _logger.LogInformation("   Checking aliases...");
        
        foreach (var answer in answers.Where(a => a.IsActive))
        {
            _logger.LogInformation("   Answer {Id} has {Count} aliases", answer.Id, answer.Aliases.Count);
            
            foreach (var alias in answer.Aliases)
            {
                _logger.LogInformation("      Comparing alias: '{Alias}' == '{User}' ?", 
                    alias.NormalizedAlias, normalizedUserAnswer);
                
                if (alias.NormalizedAlias == normalizedUserAnswer)
                {
                    _logger.LogInformation("      ✅ ALIAS MATCH! Alias ID: {AliasId}", alias.Id);

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
