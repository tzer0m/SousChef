namespace SousChef.Services;

/// <summary>
/// One step on the edit form.
/// </summary>
public class StepEditRow
{
    /// <summary>
    /// The step title; steps with no title or text are dropped.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The sub-steps as text, one "a) …" line each, with optional "TIP:" and "IMPORTANT:" lines.
    /// </summary>
    public string? Text { get; set; }
}