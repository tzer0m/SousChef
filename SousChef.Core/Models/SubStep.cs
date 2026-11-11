namespace SousChef.Core.Models;

/// <summary>
/// A lettered sub-step, such as "a) Boil a half-full kettle."
/// </summary>
public class SubStep
{
    /// <summary>
    /// The sub-step letter, or empty if the step isn't lettered.
    /// </summary>
    public string Letter { get; set; } = string.Empty;

    /// <summary>
    /// The instruction text, without any tip or important note.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The parts of the text printed in red (hot ingredients).
    /// </summary>
    public List<TextRange> HotRanges { get; set; } = [];

    /// <summary>
    /// The durations found in the text.
    /// </summary>
    public List<StepTimer> Timers { get; set; } = [];

    /// <summary>
    /// The text after "TIP:", if any.
    /// </summary>
    public string? Tip { get; set; }

    /// <summary>
    /// The text after "IMPORTANT:", if any.
    /// </summary>
    public string? Important { get; set; }

    /// <summary>
    /// Whether the sub-step runs alongside the previous one ("Meanwhile").
    /// </summary>
    public bool IsParallel { get; set; }
}