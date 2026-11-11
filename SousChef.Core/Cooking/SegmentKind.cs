namespace SousChef.Core.Cooking;

/// <summary>
/// The kind of an annotated text segment.
/// </summary>
public enum SegmentKind
{
    /// <summary>
    /// Text from the card.
    /// </summary>
    Text,

    /// <summary>
    /// A scaled amount inserted after an ingredient.
    /// </summary>
    Amount
}