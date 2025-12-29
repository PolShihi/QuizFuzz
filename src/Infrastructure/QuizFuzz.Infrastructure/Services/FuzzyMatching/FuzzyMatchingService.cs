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

    // 🔥 DEFAULT Thresholds (используются если не заданы в QuestionAnswer)
    private const decimal DefaultMinConfidenceThreshold = 0.75m;
    private const int DefaultMaxEditDistance = 2;
    
    // Старые константы (для обратной совместимости)
    private const decimal ExactMatchThreshold = 1.0m;
    private const decimal HighConfidenceThreshold = 0.9m;
    private const decimal MediumConfidenceThreshold = 0.75m;
    private const decimal LowConfidenceThreshold = 0.6m;

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
        _logger.LogInformation("   🔍 [EditDistance] Trying edit distance match...");
        MatchResult? bestMatch = null;
        var bestSimilarity = 0m;

        foreach (var answer in answers.Where(a => a.IsActive))
        {
            // 🔥 НОВОЕ: Проверяем разрешен ли fuzzy matching для этого ответа
            if (!answer.AllowFuzzyMatch)
            {
                _logger.LogInformation("   ⏭️ [EditDistance] Answer {AnswerId}: Fuzzy matching DISABLED, skipping", answer.Id);
                continue;
            }
            
            // 🔥 НОВОЕ: Получаем настройки из ответа или используем defaults
            var maxEditDistance = answer.MaxEditDistance ?? DefaultMaxEditDistance;
            var minConfidence = answer.MinConfidence ?? DefaultMinConfidenceThreshold;
            
            _logger.LogInformation("   🔍 [EditDistance] Answer {AnswerId}: '{Text}' (MaxEditDist: {MaxDist}, MinConf: {MinConf:P0})", 
                answer.Id, answer.AnswerText, maxEditDistance, minConfidence);

            var distance = LevenshteinDistance.Calculate(
                answer.NormalizedAnswer,
                normalizedUserAnswer);
            
            var similarity = LevenshteinDistance.CalculateSimilarity(
                answer.NormalizedAnswer,
                normalizedUserAnswer);

            _logger.LogInformation("      Distance: {Distance}, Similarity: {Similarity:P2}", distance, similarity);

            // 🔥 НОВОЕ: Проверяем по настройкам конкретного ответа!
            if (distance <= maxEditDistance && similarity >= minConfidence)
            {
                if (similarity > bestSimilarity)
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
                        "      ✅ [EditDistance] MATCH! Distance: {Distance} <= {MaxDist}, Similarity: {Similarity:P2} >= {MinConf:P2} for answer {AnswerId}",
                        distance, maxEditDistance, similarity, minConfidence, answer.Id);
                }
            }
            else
            {
                _logger.LogInformation(
                    "      ❌ [EditDistance] NO MATCH. Distance: {Distance} > {MaxDist} OR Similarity: {Similarity:P2} < {MinConf:P2}",
                    distance, maxEditDistance, similarity, minConfidence);
            }
        }

        return bestMatch;
    }

    private MatchResult? TryTokenMatch(
        IReadOnlyCollection<Domain.Entities.QuestionAnswer> answers,
        string normalizedUserAnswer)
    {
        _logger.LogInformation("   🔍 [Token] Trying token similarity match...");
        MatchResult? bestMatch = null;
        var bestSimilarity = 0m;

        foreach (var answer in answers.Where(a => a.IsActive))
        {
            // 🔥 НОВОЕ: Проверяем разрешен ли fuzzy matching для этого ответа
            if (!answer.AllowFuzzyMatch)
            {
                _logger.LogInformation("   ⏭️ [Token] Answer {AnswerId}: Fuzzy matching DISABLED, skipping", answer.Id);
                continue;
            }
            
            // 🔥 НОВОЕ: Получаем минимальную confidence из ответа или используем default
            var minConfidence = answer.MinConfidence ?? DefaultMinConfidenceThreshold;
            
            _logger.LogInformation("   🔍 [Token] Answer {AnswerId}: '{Text}' (MinConf: {MinConf:P0})", 
                answer.Id, answer.AnswerText, minConfidence);

            var weightedRatio = TokenSimilarity.CalculateWeightedRatio(
                answer.NormalizedAnswer,
                normalizedUserAnswer);
                
            _logger.LogInformation("      Weighted ratio: {Ratio:P2}", weightedRatio);

            // 🔥 НОВОЕ: Проверяем по настройкам конкретного ответа!
            if (weightedRatio >= minConfidence)
            {
                if (weightedRatio > bestSimilarity)
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
                        "      ✅ [Token] MATCH! Ratio: {Ratio:P2} >= {MinConf:P2} for answer {AnswerId}",
                        weightedRatio, minConfidence, answer.Id);
                }
            }
            else
            {
                _logger.LogInformation(
                    "      ❌ [Token] NO MATCH. Ratio: {Ratio:P2} < {MinConf:P2}",
                    weightedRatio, minConfidence);
            }
        }

        return bestMatch;
    }
}
