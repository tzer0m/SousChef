using System.Text.RegularExpressions;
using SousChef.Core.Models;

namespace SousChef.Core.Cooking;

/// <summary>
/// Finds ingredient mentions in instruction text, such as "diced chicken" for "Diced British Chicken Breast".
/// </summary>
public static partial class IngredientMatcher
{
    private static readonly HashSet<string> StopWords = ["diced", "sliced", "chopped", "grated", "british", "style", "paste", "puree", "clove", "cloves", "sauce", "mix", "spice", "fresh", "dried", "ground", "whole", "sachet", "pot", "and", "with", "of", "the", "free", "range", "large", "small", "medium", "baby", "mini", "for", "to", "in", "on", "a", "an"];

    private static readonly HashSet<string> Connectives = ["the", "a", "an", "and", "with", "of", "for", "to", "in", "on"];

    private static readonly Dictionary<string, string> Abbreviations = new() { ["mayo"] = "mayonnaise", ["soy"] = "soya", ["veg"] = "vegetable" };

    /// <summary>
    /// Finds every ingredient mention in the text, left to right.
    /// </summary>
    /// <param name="text">The instruction text.</param>
    /// <param name="ingredients">The recipe's ingredients.</param>
    public static List<IngredientMention> FindMentions(string text, IReadOnlyList<Ingredient> ingredients)
    {
        List<Match> tokens = [.. WordRegex().Matches(text).Cast<Match>()];
        List<string> lowerTokens = [.. tokens.Select(t => t.Value.ToLowerInvariant())];
        List<List<string>> nameTokens = [.. ingredients.Select(i => Tokenise(i.Name))];
        List<List<string>> keywords = [.. nameTokens.Select(GetKeywords)];
        List<IngredientMention> mentions = [];
        int consumed = 0;
        int index = 0;
        while (index < tokens.Count)
        {
            int best = -1;
            int bestRun = 0;
            for (int i = 0; i < ingredients.Count; i++)
            {
                int run = CountRun(lowerTokens, index, keywords[i]);
                if (run > bestRun)
                {
                    best = i;
                    bestRun = run;
                }
            }
            if (best < 0)
            {
                index++;
                continue;
            }
            int first = index;
            while (first > consumed && nameTokens[best].Any(n => IsMatch(lowerTokens[first - 1], n)))
            {
                first--;
            }
            int last = index + bestRun - 1;
            while (last + 1 < tokens.Count && nameTokens[best].Any(n => IsMatch(lowerTokens[last + 1], n)))
            {
                last++;
            }
            while (first < index && Connectives.Contains(lowerTokens[first]))
            {
                first++;
            }
            while (last > index + bestRun - 1 && Connectives.Contains(lowerTokens[last]))
            {
                last--;
            }
            mentions.Add(new IngredientMention { Start = tokens[first].Index, Length = tokens[last].Index + tokens[last].Length - tokens[first].Index, IngredientIndex = best });
            index = last + 1;
            consumed = index;
        }
        return mentions;
    }

    /// <summary>
    /// Counts how many consecutive tokens from a position match distinct keywords of one ingredient.
    /// </summary>
    /// <param name="tokens">The lower-case text tokens.</param>
    /// <param name="start">The token to start at.</param>
    /// <param name="keywords">The ingredient's keywords.</param>
    private static int CountRun(List<string> tokens, int start, List<string> keywords)
    {
        HashSet<string> used = [];
        int run = 0;
        while (start + run < tokens.Count)
        {
            string? keyword = keywords.FirstOrDefault(k => !used.Contains(k) && IsMatch(tokens[start + run], k));
            if (keyword == null)
            {
                break;
            }
            used.Add(keyword);
            run++;
        }
        return run;
    }

    /// <summary>
    /// Returns the distinctive words of an ingredient name, falling back to all of them.
    /// </summary>
    /// <param name="nameTokens">The lower-case name tokens.</param>
    private static List<string> GetKeywords(List<string> nameTokens)
    {
        List<string> keywords = [.. nameTokens.Where(t => t.Length >= 3 && !StopWords.Contains(t))];
        return keywords.Count > 0 ? keywords : nameTokens;
    }

    /// <summary>
    /// Splits text into lower-case words.
    /// </summary>
    /// <param name="text">The text to split.</param>
    private static List<string> Tokenise(string text)
    {
        return [.. WordRegex().Matches(text).Select(m => m.Value.ToLowerInvariant())];
    }

    /// <summary>
    /// Checks whether a text token refers to an ingredient word, allowing plurals and abbreviations such as "mayo".
    /// </summary>
    /// <param name="token">The lower-case text token.</param>
    /// <param name="word">The lower-case ingredient word.</param>
    private static bool IsMatch(string token, string word)
    {
        if (token == word || token + "s" == word || word + "s" == token || token + "es" == word || word + "es" == token)
        {
            return true;
        }
        return Abbreviations.TryGetValue(token, out string? full) && full == word;
    }

    [GeneratedRegex(@"[A-Za-z]+")]
    private static partial Regex WordRegex();
}