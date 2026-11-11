namespace SousChef.Core.Models;

/// <summary>
/// A duration found in a sub-step, such as "10-12 mins".
/// </summary>
public class StepTimer : TextRange
{
    /// <summary>
    /// The matched text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The shortest duration in seconds.
    /// </summary>
    public int MinSeconds { get; set; }

    /// <summary>
    /// The longest duration in seconds (same as the minimum when there's no range).
    /// </summary>
    public int MaxSeconds { get; set; }
}