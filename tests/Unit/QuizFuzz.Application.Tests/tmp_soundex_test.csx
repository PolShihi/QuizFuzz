using QuizFuzz.Infrastructure.Services.FuzzyMatching;
using System;

var result = SoundexAlgorithm.Encode("School");
Console.WriteLine($"School -> {result}");
result = SoundexAlgorithm.Encode("Skul");
Console.WriteLine($"Skul -> {result}");
result = SoundexAlgorithm.Encode("Skool");
Console.WriteLine($"Skool -> {result}");
