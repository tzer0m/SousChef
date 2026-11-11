using SousChef.Core.Models;

namespace SousChef.Core.Cooking;

/// <summary>
/// A run of instruction text with the same formatting, ready to render.
/// </summary>
public class TextSegment
{
    /// <summary>
    /// Whether this is card text or an inserted amount.
    /// </summary>
    public SegmentKind Kind { get; set; }

    /// <summary>
    /// The text to show.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Whether the card prints this text in red (hot).
    /// </summary>
    public bool IsHot { get; set; }

    /// <summary>
    /// Whether this text names an ingredient.
    /// </summary>
    public bool IsIngredient { get; set; }

    /// <summary>
    /// The timer this text describes, if any.
    /// </summary>
    public StepTimer? Timer { get; set; }
}