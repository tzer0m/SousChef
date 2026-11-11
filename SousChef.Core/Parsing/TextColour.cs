namespace SousChef.Core.Parsing;

/// <summary>
/// The colours that carry meaning on a recipe card.
/// </summary>
internal enum TextColour
{
    /// <summary>
    /// Black or near-black body text.
    /// </summary>
    Black,

    /// <summary>
    /// Mid grey, used for units and footnotes.
    /// </summary>
    Grey,

    /// <summary>
    /// Red, used for hot ingredients, spice tags and important notes.
    /// </summary>
    Red,

    /// <summary>
    /// Green, used for headings, step numbers and tips.
    /// </summary>
    Green,

    /// <summary>
    /// White, used for text on coloured panels.
    /// </summary>
    White,

    /// <summary>
    /// Any other colour.
    /// </summary>
    Other
}