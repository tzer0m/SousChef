using System.Globalization;
using System.Text.RegularExpressions;
using SousChef.Core.Models;

namespace SousChef.Core.Cooking;

/// <summary>
/// Finds durations such as "10-12 mins" in instruction text.
/// </summary>
public static partial class TimerExtractor
{
    /// <summary>
    /// Finds every duration in the text.
    /// </summary>
    /// <param name="text">The instruction text.</param>
    public static List<StepTimer> Extract(string text)
    {
        List<StepTimer> timers = [];
        foreach (Match match in DurationRegex().Matches(text))
        {
            int factor = GetFactor(match.Groups["unit"].Value);
            decimal min = decimal.Parse(match.Groups["min"].Value, CultureInfo.InvariantCulture);
            decimal max = match.Groups["max"].Success ? decimal.Parse(match.Groups["max"].Value, CultureInfo.InvariantCulture) : min;
            timers.Add(new StepTimer { Start = match.Index, Length = match.Length, Text = match.Value, MinSeconds = (int)(min * factor), MaxSeconds = (int)(Math.Max(min, max) * factor) });
        }
        return timers;
    }

    /// <summary>
    /// Returns the number of seconds in one of the given unit.
    /// </summary>
    /// <param name="unit">The unit as written, such as "mins".</param>
    private static int GetFactor(string unit)
    {
        string lower = unit.ToLowerInvariant();
        if (lower.StartsWith('h'))
        {
            return 3600;
        }
        return lower.StartsWith('s') ? 1 : 60;
    }

    [GeneratedRegex(@"(?<min>\d+(?:\.\d+)?)(?:\s*(?:-|–|to)\s*(?<max>\d+(?:\.\d+)?))?\s*(?<unit>hours?|hrs?|minutes?|mins?|seconds?|secs?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex DurationRegex();
}