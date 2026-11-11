using System.Text;
using System.Text.RegularExpressions;
using SousChef.Core.Models;

namespace SousChef.Core.Cooking;

/// <summary>
/// Converts a step's sub-steps to and from editable text ("a) …" lines, with "TIP:" and "IMPORTANT:" lines).
/// </summary>
public static partial class StepTextFormat
{
    /// <summary>
    /// Writes a step's sub-steps as editable text.
    /// </summary>
    /// <param name="step">The step.</param>
    public static string ToText(RecipeStep step)
    {
        StringBuilder text = new();
        foreach (SubStep subStep in step.SubSteps)
        {
            text.AppendLine(subStep.Letter.Length > 0 ? $"{subStep.Letter}) {subStep.Text}" : subStep.Text);
            if (subStep.Tip != null)
            {
                text.AppendLine($"TIP: {subStep.Tip}");
            }
            if (subStep.Important != null)
            {
                text.AppendLine($"IMPORTANT: {subStep.Important}");
            }
        }
        return text.ToString().TrimEnd();
    }

    /// <summary>
    /// Reads sub-steps back from edited text, keeping hot (red) phrases from the original where they still appear.
    /// </summary>
    /// <param name="text">The edited text.</param>
    /// <param name="original">The step before editing, if any.</param>
    public static List<SubStep> FromText(string text, RecipeStep? original)
    {
        List<string> hotPhrases = [.. (original?.SubSteps ?? []).SelectMany(s => s.HotRanges.Where(r => r.End <= s.Text.Length).Select(r => s.Text.Substring(r.Start, r.Length))).Distinct()];
        List<(string Letter, StringBuilder Text, string? Tip, string? Important)> parts = [];
        foreach (string rawLine in text.Replace("\r", string.Empty).Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }
            Match letter = LetterRegex().Match(line);
            if (line.StartsWith("TIP:", StringComparison.OrdinalIgnoreCase) && parts.Count > 0)
            {
                parts[^1] = parts[^1] with { Tip = line[4..].Trim() };
            }
            else if (line.StartsWith("IMPORTANT:", StringComparison.OrdinalIgnoreCase) && parts.Count > 0)
            {
                parts[^1] = parts[^1] with { Important = line[10..].Trim() };
            }
            else if (letter.Success || parts.Count == 0)
            {
                parts.Add((letter.Success ? letter.Groups["letter"].Value : string.Empty, new StringBuilder(letter.Success ? letter.Groups["text"].Value : line), null, null));
            }
            else
            {
                parts[^1].Text.Append(' ').Append(line);
            }
        }
        return [.. parts.Select(p => Build(p.Letter, p.Text.ToString().Trim(), p.Tip, p.Important, hotPhrases))];
    }

    /// <summary>
    /// Creates a sub-step, finding timers and re-marking hot phrases.
    /// </summary>
    /// <param name="letter">The sub-step letter.</param>
    /// <param name="text">The instruction text.</param>
    /// <param name="tip">The tip, if any.</param>
    /// <param name="important">The important note, if any.</param>
    /// <param name="hotPhrases">Phrases printed in red on the card.</param>
    private static SubStep Build(string letter, string text, string? tip, string? important, List<string> hotPhrases)
    {
        List<TextRange> hot = [];
        foreach (string phrase in hotPhrases)
        {
            int index = text.IndexOf(phrase, StringComparison.Ordinal);
            if (index >= 0)
            {
                hot.Add(new TextRange { Start = index, Length = phrase.Length });
            }
        }
        return new SubStep { Letter = letter, Text = text, Tip = tip, Important = important, HotRanges = [.. hot.OrderBy(h => h.Start)], Timers = TimerExtractor.Extract(text), IsParallel = text.StartsWith("Meanwhile", StringComparison.OrdinalIgnoreCase) };
    }

    [GeneratedRegex(@"^(?<letter>[a-h])\)\s*(?<text>.*)$")]
    private static partial Regex LetterRegex();
}