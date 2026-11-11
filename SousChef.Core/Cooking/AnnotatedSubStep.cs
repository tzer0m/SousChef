using SousChef.Core.Models;

namespace SousChef.Core.Cooking;

/// <summary>
/// A sub-step with its annotated text segments.
/// </summary>
public class AnnotatedSubStep
{
    /// <summary>
    /// The parsed sub-step.
    /// </summary>
    public SubStep SubStep { get; set; } = new();

    /// <summary>
    /// The text segments to render.
    /// </summary>
    public List<TextSegment> Segments { get; set; } = [];
}