namespace SousChef.Core.Models;

/// <summary>
/// A numbered step of the method.
/// </summary>
public class RecipeStep
{
    /// <summary>
    /// The step number.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// The step title, such as "Cook the Rice".
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The lettered sub-steps.
    /// </summary>
    public List<SubStep> SubSteps { get; set; } = [];

    /// <summary>
    /// The key of the step photo, if one was extracted.
    /// </summary>
    public string? ImageKey { get; set; }
}