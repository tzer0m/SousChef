using SousChef.Core.Models;

namespace SousChef.Core.Cooking;

/// <summary>
/// A step with its annotated sub-steps.
/// </summary>
public class AnnotatedStep
{
    /// <summary>
    /// The parsed step.
    /// </summary>
    public RecipeStep Step { get; set; } = new();

    /// <summary>
    /// The annotated sub-steps.
    /// </summary>
    public List<AnnotatedSubStep> SubSteps { get; set; } = [];
}