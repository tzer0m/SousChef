using SousChef.Core.Cooking;
using SousChef.Core.Models;

namespace SousChef.Tests;

/// <summary>
/// Tests for annotating instructions with scaled amounts and timers.
/// </summary>
public class StepAnnotatorTests
{
    /// <summary>
    /// The first mention of each ingredient gets its scaled amount, and "see pantry" pointers are replaced.
    /// </summary>
    [Fact]
    public void Annotate_InsertsAmountsOnFirstUse()
    {
        Recipe recipe = new()
        {
            Ingredients =
            [
                new Ingredient { Name = "Sambal Paste", Unit = "g", Quantities = new() { [2] = 15, [4] = 30 } },
                new Ingredient { Name = "Mayonnaise", Unit = "tbsp", IsPantry = true, Quantities = new() { [2] = 2, [4] = 4 } }
            ]
        };
        StepAnnotator annotator = new(recipe, 4);
        List<TextSegment> first = annotator.Annotate(new SubStep { Text = "Mix the sambal and mayo (see pantry for amount)." });
        Assert.Equal("Mix the sambal [30 g] and mayo [4 tbsp].", Render(first));
        List<TextSegment> second = annotator.Annotate(new SubStep { Text = "Drizzle over the sambal mayo." });
        Assert.Equal("Drizzle over the sambal mayo.", Render(second));
        Assert.Contains(second, s => s.IsIngredient && s.Text == "sambal");
    }

    /// <summary>
    /// Durations become timer segments.
    /// </summary>
    [Fact]
    public void Annotate_MarksTimers()
    {
        string text = "Stir-fry until golden, 7-8 mins.";
        StepAnnotator annotator = new(new Recipe(), 2);
        List<TextSegment> segments = annotator.Annotate(new SubStep { Text = text, Timers = TimerExtractor.Extract(text) });
        TextSegment timer = Assert.Single(segments, s => s.Timer != null);
        Assert.Equal("7-8 mins", timer.Text);
    }

    /// <summary>
    /// Ingredient mentions take in surrounding words from the ingredient's name.
    /// </summary>
    [Fact]
    public void FindMentions_ExtendsOverNameWords()
    {
        List<Ingredient> ingredients = [new Ingredient { Name = "Diced British Chicken Breast" }, new Ingredient { Name = "Indonesian Style Spice Mix" }];
        string text = "add the diced chicken and the Indonesian style spice mix, then stir";
        List<IngredientMention> mentions = IngredientMatcher.FindMentions(text, ingredients);
        Assert.Equal(["diced chicken", "Indonesian style spice mix"], mentions.Select(m => text.Substring(m.Start, m.Length)));
    }

    /// <summary>
    /// Renders segments as plain text with amounts in brackets.
    /// </summary>
    /// <param name="segments">The segments.</param>
    private static string Render(List<TextSegment> segments)
    {
        return string.Concat(segments.Select(s => s.Kind == SegmentKind.Amount ? $" [{s.Text}]" : s.Text));
    }
}