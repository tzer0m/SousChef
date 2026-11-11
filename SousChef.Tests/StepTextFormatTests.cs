using SousChef.Core.Cooking;
using SousChef.Core.Models;

namespace SousChef.Tests;

/// <summary>
/// Tests for editing steps as text.
/// </summary>
public class StepTextFormatTests
{
    /// <summary>
    /// Steps survive a round trip through text, keeping tips, important notes, timers and hot phrases.
    /// </summary>
    [Fact]
    public void RoundTrip_KeepsStructure()
    {
        RecipeStep step = new()
        {
            SubSteps =
            [
                new SubStep { Letter = "a", Text = "Add the sambal and stir, 2 mins.", HotRanges = [new TextRange { Start = 8, Length = 6 }], Tip = "Go easy." },
                new SubStep { Letter = "b", Text = "Serve.", Important = "Check it's hot." }
            ]
        };
        string text = StepTextFormat.ToText(step);
        Assert.Equal("a) Add the sambal and stir, 2 mins.\nTIP: Go easy.\nb) Serve.\nIMPORTANT: Check it's hot.", text.Replace("\r", string.Empty));
        List<SubStep> edited = StepTextFormat.FromText(text.Replace("stir", "stir well"), step);
        Assert.Equal(2, edited.Count);
        Assert.Equal("Add the sambal and stir well, 2 mins.", edited[0].Text);
        Assert.Equal("Go easy.", edited[0].Tip);
        Assert.Equal("Check it's hot.", edited[1].Important);
        Assert.Single(edited[0].Timers);
        TextRange hot = Assert.Single(edited[0].HotRanges);
        Assert.Equal("sambal", edited[0].Text.Substring(hot.Start, hot.Length));
    }

    /// <summary>
    /// Lines without a letter continue the previous sub-step.
    /// </summary>
    [Fact]
    public void FromText_JoinsContinuationLines()
    {
        List<SubStep> subSteps = StepTextFormat.FromText("a) Boil the kettle\nand wait.\nb) Pour.", null);
        Assert.Equal(["Boil the kettle and wait.", "Pour."], subSteps.Select(s => s.Text));
    }
}