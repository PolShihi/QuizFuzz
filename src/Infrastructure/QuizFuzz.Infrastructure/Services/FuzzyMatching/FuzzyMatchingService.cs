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

    //  DEFAULT Thresholds (используются если не заданы в QuestionAnswer)
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
        _logger.LogDebug(" [FuzzyMatching] ========== EVALUATING ANSWER ==========");
        _logger.LogDebug(" [FuzzyMatching] Question ID: {QuestionId}", questionId);
        _logger.LogDebug(" [FuzzyMatching] User Answer: '{UserAnswer}'", userAnswer);
        
        if (string.IsNullOrWhiteSpace(userAnswer))
        {
            _logger.LogDebug(" [FuzzyMatching] Answer is empty!");
            return new MatchResult
            {
                IsCorrect = false,
                Strategy = MatchStrategy.Rejected,
                Confidence = 0m,
                NormalizedAnswer = string.Empty
            };
        }

        // Получаем вопрос с ответами и алиасами
        _logger.LogDebug(" [FuzzyMatching] Loading question with answers and aliases...");
        var question = await _unitOfWork.Questions.GetWithAllDetailsAsync(questionId, cancellationToken);
        if (question == null)
        {
            _logger.LogDebug(" [FuzzyMatching] Question {QuestionId} not found!", questionId);
            throw new InvalidOperationException($"Question {questionId} not found");
        }

        _logger.LogDebug(" [FuzzyMatching] Question loaded: '{QuestionText}'", question.PromptText);
        _logger.LogDebug(" [FuzzyMatching] Question has {Count} answers", question.Answers.Count);
        
        foreach (var ans in question.Answers)
        {
            _logger.LogDebug("   Answer {Id}: '{Text}' (IsPrimary: {IsPrimary}, IsActive: {IsActive}, Aliases: {AliasCount})", 
                ans.Id, ans.AnswerText, ans.IsPrimary, ans.IsActive, ans.Aliases.Count);
            
            foreach (var alias in ans.Aliases)
            {
                _logger.LogDebug("      Alias: '{AliasText}' → '{NormalizedAlias}'", 
                    alias.AliasText, alias.NormalizedAlias);
            }
        }

        var normalizedUserAnswer = TextNormalizer.Normalize(userAnswer);
        _logger.LogDebug(" [FuzzyMatching] Normalized user answer: '{Normalized}'", normalizedUserAnswer);

        // 1. Exact match (нормализованный)
        _logger.LogDebug(" [FuzzyMatching] Step 1: Trying EXACT match...");
        var exactMatch = await TryExactMatchAsync(question.Answers, normalizedUserAnswer);
        if (exactMatch != null)
        {
            _logger.LogDebug(" [FuzzyMatching] EXACT MATCH FOUND!");
            return exactMatch;
        }
        _logger.LogDebug("⏭ [FuzzyMatching] No exact match");

        // 2. Alias match
        _logger.LogDebug(" [FuzzyMatching] Step 2: Trying ALIAS match...");
        var aliasMatch = await TryAliasMatchAsync(question.Answers, normalizedUserAnswer);
        if (aliasMatch != null)
        {
            _logger.LogDebug(" [FuzzyMatching] ALIAS MATCH FOUND!");
            return aliasMatch;
        }
        _logger.LogDebug("⏭ [FuzzyMatching] No alias match");

        // 3. Edit distance (Levenshtein)
        _logger.LogDebug(" [FuzzyMatching] Step 3: Trying EDIT DISTANCE match...");
        var editDistanceMatch = TryEditDistanceMatch(question.Answers, normalizedUserAnswer);
        if (editDistanceMatch != null)
        {
            _logger.LogDebug(" [FuzzyMatching] EDIT DISTANCE MATCH FOUND!");
            return editDistanceMatch;
        }
        _logger.LogDebug("⏭ [FuzzyMatching] No edit distance match");

        // 4.  НОВОЕ: Transliteration match
        _logger.LogDebug(" [FuzzyMatching] Step 4: Trying TRANSLITERATION match...");
        var translitMatch = TryTranslitMatch(question.Answers, normalizedUserAnswer);
        if (translitMatch != null)
        {
            _logger.LogDebug(" [FuzzyMatching] TRANSLITERATION MATCH FOUND!");
            return translitMatch;
        }
        _logger.LogDebug("⏭ [FuzzyMatching] No transliteration match");

        // 5. Token similarity
        _logger.LogDebug(" [FuzzyMatching] Step 5: Trying TOKEN match...");
        var tokenMatch = TryTokenMatch(question.Answers, normalizedUserAnswer);
        if (tokenMatch != null)
        {
            _logger.LogDebug(" [FuzzyMatching] TOKEN MATCH FOUND!");
            return tokenMatch;
        }
        _logger.LogDebug("⏭ [FuzzyMatching] No token match");

        // 6.  НОВОЕ: Phonetic match (последний шаг - самый мягкий)
        _logger.LogDebug(" [FuzzyMatching] Step 6: Trying PHONETIC match...");
        var phoneticMatch = TryPhoneticMatch(question.Answers, normalizedUserAnswer);
        if (phoneticMatch != null)
        {
            _logger.LogDebug(" [FuzzyMatching] PHONETIC MATCH FOUND!");
            return phoneticMatch;
        }
        _logger.LogDebug("⏭ [FuzzyMatching] No phonetic match");

        // 7. No match
        _logger.LogDebug(" [FuzzyMatching] NO MATCH FOUND - Answer is INCORRECT");
        _logger.LogDebug(" [FuzzyMatching] ========== EVALUATION COMPLETE: INCORRECT ==========");
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
        _logger.LogDebug("   Checking {Count} active answers for exact match...", answers.Count(a => a.IsActive));
        
        foreach (var answer in answers.Where(a => a.IsActive))
        {
            _logger.LogDebug("   Comparing: '{Correct}' == '{User}' ?", 
                answer.NormalizedAnswer, normalizedUserAnswer);
            
            if (answer.NormalizedAnswer == normalizedUserAnswer)
            {
                _logger.LogDebug("    EXACT MATCH! Answer ID: {AnswerId}", answer.Id);

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
                _logger.LogDebug("    Not equal");
            }
        }

        return null;
    }

    private async Task<MatchResult?> TryAliasMatchAsync(
        IReadOnlyCollection<Domain.Entities.QuestionAnswer> answers,
        string normalizedUserAnswer)
    {
        _logger.LogDebug("   Checking aliases...");
        
        foreach (var answer in answers.Where(a => a.IsActive))
        {
            _logger.LogDebug("   Answer {Id} has {Count} aliases", answer.Id, answer.Aliases.Count);
            
            foreach (var alias in answer.Aliases)
            {
                _logger.LogDebug("      Comparing alias: '{Alias}' == '{User}' ?", 
                    alias.NormalizedAlias, normalizedUserAnswer);
                
                if (alias.NormalizedAlias == normalizedUserAnswer)
                {
                    _logger.LogDebug("       ALIAS MATCH! Alias ID: {AliasId}", alias.Id);

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
        _logger.LogDebug("    [EditDistance] Trying edit distance match...");
        MatchResult? bestMatch = null;
        var bestSimilarity = 0m;

        foreach (var answer in answers.Where(a => a.IsActive))
        {
            //  НОВОЕ: Проверяем разрешен ли fuzzy matching для этого ответа
            if (!answer.AllowFuzzyMatch)
            {
                _logger.LogDebug("   ⏭ [EditDistance] Answer {AnswerId}: Fuzzy matching DISABLED, skipping", answer.Id);
                continue;
            }
            
            //  НОВОЕ: Получаем настройки из ответа или используем defaults
            var maxEditDistance = answer.MaxEditDistance ?? DefaultMaxEditDistance;
            var minConfidence = answer.MinConfidence ?? DefaultMinConfidenceThreshold;
            
            _logger.LogDebug("    [EditDistance] Answer {AnswerId}: '{Text}' (MaxEditDist: {MaxDist}, MinConf: {MinConf:P0})", 
                answer.Id, answer.AnswerText, maxEditDistance, minConfidence);

            var distance = LevenshteinDistance.Calculate(
                answer.NormalizedAnswer,
                normalizedUserAnswer);
            
            var similarity = LevenshteinDistance.CalculateSimilarity(
                answer.NormalizedAnswer,
                normalizedUserAnswer);

            _logger.LogDebug("      Distance: {Distance}, Similarity: {Similarity:P2}", distance, similarity);

            //  НОВОЕ: Проверяем по настройкам конкретного ответа!
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

                    _logger.LogDebug(
                        "       [EditDistance] MATCH! Distance: {Distance} <= {MaxDist}, Similarity: {Similarity:P2} >= {MinConf:P2} for answer {AnswerId}",
                        distance, maxEditDistance, similarity, minConfidence, answer.Id);
                }
            }
            else
            {
                _logger.LogDebug(
                    "       [EditDistance] NO MATCH. Distance: {Distance} > {MaxDist} OR Similarity: {Similarity:P2} < {MinConf:P2}",
                    distance, maxEditDistance, similarity, minConfidence);
            }
        }

        return bestMatch;
    }

    private MatchResult? TryTokenMatch(
        IReadOnlyCollection<Domain.Entities.QuestionAnswer> answers,
        string normalizedUserAnswer)
    {
        _logger.LogDebug("    [Token] Trying token similarity match...");
        MatchResult? bestMatch = null;
        var bestSimilarity = 0m;

        foreach (var answer in answers.Where(a => a.IsActive))
        {
            //  НОВОЕ: Проверяем разрешен ли fuzzy matching для этого ответа
            if (!answer.AllowFuzzyMatch)
            {
                _logger.LogDebug("   ⏭ [Token] Answer {AnswerId}: Fuzzy matching DISABLED, skipping", answer.Id);
                continue;
            }
            
            //  НОВОЕ: Получаем минимальную confidence из ответа или используем default
            var minConfidence = answer.MinConfidence ?? DefaultMinConfidenceThreshold;
            
            _logger.LogDebug("    [Token] Answer {AnswerId}: '{Text}' (MinConf: {MinConf:P0})", 
                answer.Id, answer.AnswerText, minConfidence);

            var weightedRatio = TokenSimilarity.CalculateWeightedRatio(
                answer.NormalizedAnswer,
                normalizedUserAnswer);
                
            _logger.LogDebug("      Weighted ratio: {Ratio:P2}", weightedRatio);

            //  НОВОЕ: Проверяем по настройкам конкретного ответа!
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

                    _logger.LogDebug(
                        "       [Token] MATCH! Ratio: {Ratio:P2} >= {MinConf:P2} for answer {AnswerId}",
                        weightedRatio, minConfidence, answer.Id);
                }
            }
            else
            {
                _logger.LogDebug(
                    "       [Token] NO MATCH. Ratio: {Ratio:P2} < {MinConf:P2}",
                    weightedRatio, minConfidence);
            }
        }

        return bestMatch;
    }
    
    /// <summary>
    ///  НОВОЕ: Транслитерация + Levenshtein
    /// Конвертирует оба текста в латиницу и сравнивает
    /// </summary>
    private MatchResult? TryTranslitMatch(
        IReadOnlyCollection<Domain.Entities.QuestionAnswer> answers,
        string normalizedUserAnswer)
    {
        _logger.LogDebug("    [Translit] Trying transliteration match...");
        
        // Транслитерируем ответ пользователя
        var userTranslit = Transliterator.ToLatin(normalizedUserAnswer);
        _logger.LogDebug("      User answer transliterated: '{Original}' → '{Translit}'", 
            normalizedUserAnswer, userTranslit);
        
        MatchResult? bestMatch = null;
        var bestSimilarity = 0m;
        
        foreach (var answer in answers.Where(a => a.IsActive))
        {
            //  Проверяем разрешен ли fuzzy matching для этого ответа
            if (!answer.AllowFuzzyMatch)
            {
                _logger.LogDebug("      ⏭ [Translit] Answer {AnswerId}: Fuzzy matching DISABLED, skipping", answer.Id);
                continue;
            }
            
            //  Получаем настройки из ответа или используем defaults
            var maxEditDistance = answer.MaxEditDistance ?? DefaultMaxEditDistance;
            var minConfidence = answer.MinConfidence ?? DefaultMinConfidenceThreshold;
            
            // Транслитерируем правильный ответ
            var answerTranslit = Transliterator.ToLatin(answer.NormalizedAnswer);
            
            _logger.LogDebug("      Answer '{Original}' → '{Translit}' (MaxEditDist: {MaxDist}, MinConf: {MinConf:P0})", 
                answer.AnswerText, answerTranslit, maxEditDistance, minConfidence);
            
            var distance = LevenshteinDistance.Calculate(answerTranslit, userTranslit);
            var similarity = LevenshteinDistance.CalculateSimilarity(answerTranslit, userTranslit);
            
            _logger.LogDebug("         Distance: {Distance}, Similarity: {Similarity:P2}", distance, similarity);
            
            //  Проверяем по ИНДИВИДУАЛЬНЫМ настройкам!
            if (distance <= maxEditDistance && similarity >= minConfidence)
            {
                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                    bestMatch = new MatchResult
                    {
                        IsCorrect = true,
                        Strategy = MatchStrategy.Transliteration,
                        Confidence = similarity,
                        NormalizedAnswer = normalizedUserAnswer,
                        MatchedQuestionAnswerId = answer.Id
                    };
                    
                    _logger.LogDebug(
                        "          [Translit] MATCH via transliteration! Distance: {Distance} <= {MaxDist}, Similarity: {Similarity:P2} >= {MinConf:P2}",
                        distance, maxEditDistance, similarity, minConfidence);
                }
            }
            else
            {
                _logger.LogDebug(
                    "          [Translit] NO MATCH. Distance: {Distance} > {MaxDist} OR Similarity: {Similarity:P2} < {MinConf:P2}",
                    distance, maxEditDistance, similarity, minConfidence);
            }
        }
        
        return bestMatch;
    }
    
    /// <summary>
    ///  НОВОЕ: Фонетическое сравнение (Soundex + Metaphone)
    /// Транслитерирует в латиницу, затем применяет фонетические алгоритмы
    /// </summary>
    private MatchResult? TryPhoneticMatch(
        IReadOnlyCollection<Domain.Entities.QuestionAnswer> answers,
        string normalizedUserAnswer)
    {
        _logger.LogDebug("    [Phonetic] Trying phonetic match (Soundex + Metaphone)...");
        
        // Транслитерируем ответ пользователя
        var userTranslit = Transliterator.ToLatin(normalizedUserAnswer);
        _logger.LogDebug("      User answer transliterated for phonetic: '{Original}' → '{Translit}'", 
            normalizedUserAnswer, userTranslit);
        
        MatchResult? bestMatch = null;
        var bestConfidence = 0m;
        
        foreach (var answer in answers.Where(a => a.IsActive))
        {
            //  Проверяем разрешен ли fuzzy matching для этого ответа
            if (!answer.AllowFuzzyMatch)
            {
                _logger.LogDebug("      ⏭ [Phonetic] Answer {AnswerId}: Fuzzy matching DISABLED, skipping", answer.Id);
                continue;
            }
            
            //  Получаем минимальную confidence из ответа или используем default
            // Для фонетики используем чуть более низкий порог (т.к. это последний шаг)
            var minConfidence = (answer.MinConfidence ?? DefaultMinConfidenceThreshold) * 0.9m;
            
            // Транслитерируем правильный ответ
            var answerTranslit = Transliterator.ToLatin(answer.NormalizedAnswer);
            
            _logger.LogDebug("      Answer '{Original}' → '{Translit}' (MinConf: {MinConf:P0})", 
                answer.AnswerText, answerTranslit, minConfidence);
            
            // Фонетическое сравнение
            var phoneticConfidence = PhoneticMatcher.CalculateSimilarity(userTranslit, answerTranslit);
            
            _logger.LogDebug("         Phonetic confidence: {Confidence:P2}", phoneticConfidence);
            
            //  Проверяем по порогу
            if (phoneticConfidence >= minConfidence)
            {
                if (phoneticConfidence > bestConfidence)
                {
                    bestConfidence = phoneticConfidence;
                    bestMatch = new MatchResult
                    {
                        IsCorrect = true,
                        Strategy = MatchStrategy.Phonetic,
                        Confidence = phoneticConfidence,
                        NormalizedAnswer = normalizedUserAnswer,
                        MatchedQuestionAnswerId = answer.Id
                    };
                    
                    _logger.LogDebug(
                        "          [Phonetic] MATCH via phonetic algorithms! Confidence: {Confidence:P2} >= {MinConf:P2}",
                        phoneticConfidence, minConfidence);
                }
            }
            else
            {
                _logger.LogDebug(
                    "          [Phonetic] NO MATCH. Confidence: {Confidence:P2} < {MinConf:P2}",
                    phoneticConfidence, minConfidence);
            }
        }
        
        return bestMatch;
    }
}
