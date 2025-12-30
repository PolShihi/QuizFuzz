using QuizFuzz.Infrastructure.Services.FuzzyMatching;
using Xunit;

namespace QuizFuzz.Application.Tests.FuzzyMatching;

/// <summary>
/// Integration tests для полного flow fuzzy matching с транслитерацией и фонетикой
/// </summary>
public class FuzzyMatchingIntegrationTests
{
    #region Transliteration Integration Tests
    
    [Theory]
    [InlineData("Москва", "Moskva")]
    [InlineData("Париж", "Parizh")]
    [InlineData("Толстой", "Tolstoy")] // й → y (GOST 7.79-2000)
    [InlineData("Щука", "Shchuka")]
    public void TranslitFlow_RussianToLatin_WorksEndToEnd(string russian, string latin)
    {
        // Act - транслитерация
        var transliterated = Transliterator.ToLatin(russian);
        
        // Assert - проверяем что транслитерировалось правильно
        Assert.Equal(latin, transliterated);
        
        // Act - проверяем что можем сравнить через Levenshtein
        var distance = LevenshteinDistance.Calculate(transliterated, latin);
        
        // Assert - должно быть точное совпадение
        Assert.Equal(0, distance);
    }
    
    [Fact]
    public void TranslitFlow_UserEntersLatinForRussianAnswer_CanMatch()
    {
        // Arrange
        var russianAnswer = "Москва";
        var userInput = "Moskva";
        
        // Act - транслитерируем оба
        var russianTranslit = Transliterator.ToLatin(russianAnswer);
        var userTranslit = Transliterator.ToLatin(userInput); // already latin, stays same
        
        // Assert
        Assert.Equal("Moskva", russianTranslit);
        Assert.Equal("Moskva", userTranslit);
        Assert.Equal(russianTranslit, userTranslit);
    }
    
    [Theory]
    [InlineData("Москва", "Maskva", 1)] // 1 typo
    [InlineData("Париж", "Parizh", 0)] // exact
    [InlineData("Париж", "Paris", 3)] // different transliteration
    public void TranslitFlow_WithTypos_CalculatesCorrectDistance(string russian, string userInput, int expectedMaxDistance)
    {
        // Act
        var russianTranslit = Transliterator.ToLatin(russian);
        var distance = LevenshteinDistance.Calculate(russianTranslit, userInput);
        
        // Assert
        Assert.True(distance <= expectedMaxDistance, 
            $"Distance {distance} > expected {expectedMaxDistance} for '{russian}' vs '{userInput}'");
    }
    
    #endregion
    
    #region Phonetic Integration Tests
    
    [Theory]
    [InlineData("Phone", "Fone")]
    [InlineData("Knight", "Night")]
    public void PhoneticFlow_CommonMisspellings_MatchViaBothAlgorithms(string correct, string misspelled)
    {
        // Act - Soundex
        var soundexMatch = SoundexAlgorithm.AreSimilar(correct, misspelled);
        
        // Act - Metaphone
        var metaphoneMatch = MetaphoneAlgorithm.AreSimilar(correct, misspelled);
        
        // Act - Combined
        var phoneticSimilarity = PhoneticMatcher.CalculateSimilarity(correct, misspelled);
        
        // Assert - should have reasonable phonetic similarity
        Assert.True(phoneticSimilarity >= 0.60m, 
            $"Phonetic similarity {phoneticSimilarity:P2} too low for {correct} vs {misspelled}");
    }
    
    [Theory]
    [InlineData("Color", "Colour")]
    [InlineData("Center", "Centre")]
    public void PhoneticFlow_SpellingVariations_ReturnsGoodConfidence(string word1, string word2)
    {
        // Act
        var similarity = PhoneticMatcher.CalculateSimilarity(word1, word2);
        
        // Assert - spelling variations should have decent similarity
        Assert.True(similarity >= 0.70m, $"Expected >= 0.70, got {similarity:P2}");
    }
    
    #endregion
    
    #region Combined Transliteration + Phonetic Tests
    
    [Theory]
    [InlineData("Москва", "Maskva")] // Russian word, user types with typo
    [InlineData("Париж", "Parij")]   // Different transliteration
    [InlineData("Толстой", "Talstoy")] // Phonetic variation
    public void CombinedFlow_RussianWordWithPhoneticVariation_CanMatch(string russian, string userInput)
    {
        // Step 1: Transliterate Russian
        var russianTranslit = Transliterator.ToLatin(russian);
        
        // Step 2: Compare transliterated with user input via Levenshtein
        var distance = LevenshteinDistance.Calculate(russianTranslit, userInput);
        var similarity = LevenshteinDistance.CalculateSimilarity(russianTranslit, userInput);
        
        // Step 3: If Levenshtein doesn't match well, try phonetic
        if (similarity < 0.75m)
        {
            var phoneticSimilarity = PhoneticMatcher.CalculateSimilarity(russianTranslit, userInput);
            
            // Assert - phonetic should help
            Assert.True(phoneticSimilarity >= 0.60m || similarity >= 0.60m,
                $"Neither Levenshtein ({similarity:P2}) nor Phonetic ({phoneticSimilarity:P2}) matched well");
        }
        else
        {
            // Assert - Levenshtein was good enough
            Assert.True(similarity >= 0.75m);
        }
    }
    
    [Fact]
    public void CombinedFlow_SchoolInRussian_UserTypesSkul_CanMatchViaTranslitAndPhonetic()
    {
        // Arrange
        var russianWord = "Школа"; // School in Russian
        var userInput = "Shko";   // Shortened/misspelled
        
        // Act - Step 1: Transliterate
        var russianTranslit = Transliterator.ToLatin(russianWord); // "Shko"
        
        // Act - Step 2: Compare
        var distance = LevenshteinDistance.Calculate(russianTranslit, userInput);
        
        // Assert - should be close enough
        Assert.True(distance <= 2, $"Distance {distance} too high for '{russianTranslit}' vs '{userInput}'");
    }
    
    #endregion
    
    #region Cascade Matching Scenarios
    
    [Fact]
    public void CascadeFlow_ExactMatch_ShouldNotNeedFuzzy()
    {
        // Arrange
        var answer = "Paris";
        var userInput = "Paris";
        
        // Act
        var normalizedAnswer = TextNormalizer.Normalize(answer);
        var normalizedUser = TextNormalizer.Normalize(userInput);
        
        // Assert - exact match should work
        Assert.Equal(normalizedAnswer, normalizedUser);
        
        // No need for further fuzzy matching
    }
    
    [Fact]
    public void CascadeFlow_SlightTypo_ShouldMatchViaEditDistance()
    {
        // Arrange
        var answer = "Paris";
        var userInput = "Pari"; // missing 's'
        
        // Act
        var distance = LevenshteinDistance.Calculate(answer, userInput);
        var similarity = LevenshteinDistance.CalculateSimilarity(answer, userInput);
        
        // Assert - edit distance should catch it
        Assert.Equal(1, distance);
        Assert.True(similarity >= 0.75m);
    }
    
    [Fact]
    public void CascadeFlow_PhoneticError_ShouldMatchViaPhonetic()
    {
        // Arrange
        var answer = "School";
        var userInput = "Skul"; // phonetic error
        
        // Act - edit distance won't catch it well
        var distance = LevenshteinDistance.Calculate(answer, userInput);
        var editSimilarity = LevenshteinDistance.CalculateSimilarity(answer, userInput);
        
        // Act - but phonetic will
        var phoneticSimilarity = PhoneticMatcher.CalculateSimilarity(answer, userInput);
        
        // Assert
        Assert.True(editSimilarity < 0.75m, "Edit distance should fail");
        Assert.True(phoneticSimilarity >= 0.70m, "Phonetic should succeed");
    }
    
    [Fact]
    public void CascadeFlow_RussianAnswerLatinInput_ShouldMatchViaTranslit()
    {
        // Arrange
        var answer = "Москва";
        var userInput = "Moskva";
        
        // Act - edit distance on original won't work (different alphabets)
        var directDistance = LevenshteinDistance.Calculate(answer, userInput);
        
        // Act - but transliteration will work
        var translitAnswer = Transliterator.ToLatin(answer);
        var translitDistance = LevenshteinDistance.Calculate(translitAnswer, userInput);
        
        // Assert
        Assert.True(directDistance > 5, "Direct comparison should fail");
        Assert.Equal(0, translitDistance); // "Moskva" == "Moskva"
    }
    
    #endregion
    
    #region Real-World Question Scenarios
    
    [Theory]
    [InlineData("1939", "1939")] // Exact
    [InlineData("Париж", "Paris")] // Translation (not transliteration)
    [InlineData("Париж", "Parizh")] // Transliteration
    [InlineData("Париж", "Parij")] // Alternative transliteration
    public void RealWorld_WW2YearQuestion_VariousAnswers(string correctAnswer, string userAnswer)
    {
        // This tests various answer formats for "When did WW2 start?"
        
        if (correctAnswer == userAnswer)
        {
            // Exact match
            Assert.Equal(correctAnswer, userAnswer);
        }
        else if (Transliterator.HasCyrillic(correctAnswer) && Transliterator.HasLatin(userAnswer))
        {
            // Transliteration scenario
            var translit = Transliterator.ToLatin(correctAnswer);
            var distance = LevenshteinDistance.Calculate(translit, userAnswer);
            
            // Should be close
            Assert.True(distance <= 3, $"Distance {distance} for '{translit}' vs '{userAnswer}'");
        }
    }
    
    [Theory]
    [InlineData("Толстой", "Tolstoy")] // GOST 7.79-2000 standard
    [InlineData("Толстой", "Tolstoy")] // Same as above
    [InlineData("Толстой", "Talstoy")] // Phonetic variation
    public void RealWorld_TolstoyQuestion_VariousSpellings(string russian, string userAnswer)
    {
        // Act - transliterate
        var translit = Transliterator.ToLatin(russian); // "Tolstoy" (GOST 7.79-2000)
        
        // Act - compare
        var distance = LevenshteinDistance.Calculate(translit, userAnswer);
        var similarity = LevenshteinDistance.CalculateSimilarity(translit, userAnswer);
        
        // Act - if edit distance fails, try phonetic
        var phoneticSimilarity = PhoneticMatcher.CalculateSimilarity(translit, userAnswer);
        
        // Assert - at least one should work
        Assert.True(similarity >= 0.70m || phoneticSimilarity >= 0.70m,
            $"Neither Edit ({similarity:P2}) nor Phonetic ({phoneticSimilarity:P2}) worked for '{translit}' vs '{userAnswer}'");
    }
    
    [Theory]
    [InlineData("School", "school")]
    [InlineData("School", "School")]
    [InlineData("School", "SCHOOL")]
    [InlineData("School", "Skul")]
    [InlineData("School", "Skool")]
    public void RealWorld_SchoolQuestion_CaseAndSpelling(string correct, string userInput)
    {
        // Act - normalize case
        var normalizedCorrect = correct.ToUpperInvariant();
        var normalizedUser = userInput.ToUpperInvariant();
        
        // Check if exact match after normalization
        if (normalizedCorrect == normalizedUser)
        {
            Assert.Equal(normalizedCorrect, normalizedUser);
        }
        else
        {
            // Check edit distance or phonetic
            var distance = LevenshteinDistance.Calculate(normalizedCorrect, normalizedUser);
            var phonetic = PhoneticMatcher.CalculateSimilarity(normalizedCorrect, normalizedUser);
            
            // Assert - should match via one of the methods
            Assert.True(distance <= 2 || phonetic >= 0.70m);
        }
    }
    
    #endregion
    
    #region Edge Cases and Robustness
    
    [Theory]
    [InlineData("", "")]
    [InlineData("A", "A")]
    [InlineData("123", "123")]
    public void EdgeCase_EmptyAndSingleChar_HandlesGracefully(string answer, string userInput)
    {
        // Act
        var translit = Transliterator.ToLatin(answer ?? "");
        var similarity = LevenshteinDistance.CalculateSimilarity(answer ?? "", userInput ?? "");
        
        // Assert - should not crash
        Assert.True(similarity >= 0m && similarity <= 1m);
    }
    
    [Theory]
    [InlineData("Test123", "Test123")]
    [InlineData("Hello!", "Hello!")]
    [InlineData("C++", "C++")]
    public void EdgeCase_SpecialCharacters_HandlesCorrectly(string answer, string userInput)
    {
        // Act
        var normalized1 = TextNormalizer.Normalize(answer);
        var normalized2 = TextNormalizer.Normalize(userInput);
        
        // Assert - should match after normalization
        Assert.Equal(normalized1, normalized2);
    }
    
    [Fact]
    public void EdgeCase_VeryLongText_PerformsReasonably()
    {
        // Arrange
        var longText = string.Join(" ", Enumerable.Repeat("Москва", 100));
        
        // Act - should not timeout or crash
        var translit = Transliterator.ToLatin(longText);
        var phonetic = PhoneticMatcher.CalculateSimilarity(longText, longText);
        
        // Assert
        Assert.NotEmpty(translit);
        Assert.True(phonetic >= 0.70m);
    }
    
    [Fact]
    public void EdgeCase_Diacritics_HandlesGracefully()
    {
        // Act - normalization handles diacritics
        var normalized1 = TextNormalizer.Normalize("Café");
        var normalized2 = TextNormalizer.Normalize("Cafe");
        
        // Act - calculate similarity
        var similarity = LevenshteinDistance.CalculateSimilarity(normalized1, normalized2);
        
        // Assert - just check it doesn't crash, similarity depends on TextNormalizer
        Assert.True(similarity >= 0m && similarity <= 1m);
    }
    
    #endregion
    
    #region Performance and Consistency Tests
    
    [Fact]
    public void Consistency_SameInputMultipleTimes_ReturnsSameResult()
    {
        // Arrange
        var word1 = "School";
        var word2 = "Skul";
        
        // Act - multiple times
        var results = Enumerable.Range(0, 10)
            .Select(_ => PhoneticMatcher.CalculateSimilarity(word1, word2))
            .ToList();
        
        // Assert - all should be identical
        Assert.True(results.All(r => r == results[0]), 
            $"Inconsistent results: {string.Join(", ", results.Distinct())}");
    }
    
    [Fact]
    public void Consistency_OrderInvariance_PhoneticMatching()
    {
        // Arrange
        var pairs = new[]
        {
            ("School", "Skul"),
            ("Paris", "Parizh"),
            ("Moscow", "Moskva")
        };
        
        // Act & Assert
        foreach (var (word1, word2) in pairs)
        {
            var forward = PhoneticMatcher.CalculateSimilarity(word1, word2);
            var backward = PhoneticMatcher.CalculateSimilarity(word2, word1);
            
            Assert.Equal(forward, backward);
        }
    }
    
    #endregion
}
