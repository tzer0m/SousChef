namespace SousChef.Core.Parsing;

/// <summary>
/// A step found in a scanned column: its title and body lines.
/// </summary>
/// <param name="title">The step title.</param>
/// <param name="titleY">The Y position of the title.</param>
/// <param name="startY">The Y position of the first sub-step.</param>
/// <param name="upperLimitY">The Y position of the previous step's last line in the column, or the page top.</param>
internal class StepBlock(string title, double titleY, double startY, double upperLimitY)
{
    /// <summary>
    /// The step title.
    /// </summary>
    public string Title => title;

    /// <summary>
    /// The Y position of the title.
    /// </summary>
    public double TitleY => titleY;

    /// <summary>
    /// The Y position of the first sub-step.
    /// </summary>
    public double StartY => startY;

    /// <summary>
    /// The Y position the step photo can't go above.
    /// </summary>
    public double UpperLimitY => upperLimitY;

    /// <summary>
    /// The step's body lines, top to bottom.
    /// </summary>
    public List<List<PdfWord>> Lines { get; } = [];
}