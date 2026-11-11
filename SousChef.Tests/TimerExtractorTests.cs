using SousChef.Core.Cooking;
using SousChef.Core.Models;

namespace SousChef.Tests;

/// <summary>
/// Tests for finding durations in instructions.
/// </summary>
public class TimerExtractorTests
{
    /// <summary>
    /// Durations and ranges in different units are found.
    /// </summary>
    /// <param name="text">The instruction text.</param>
    /// <param name="min">The expected minimum seconds.</param>
    /// <param name="max">The expected maximum seconds.</param>
    [Theory]
    [InlineData("cook for 10-12 mins.", 600, 720)]
    [InlineData("until fragrant, 2 mins.", 120, 120)]
    [InlineData("roast for 20 to 25 minutes", 1200, 1500)]
    [InlineData("rest for 30 secs", 30, 30)]
    [InlineData("simmer for 1 hour", 3600, 3600)]
    [InlineData("cook for 1–2 mins", 60, 120)]
    public void Extract_FindsDuration(string text, int min, int max)
    {
        StepTimer timer = Assert.Single(TimerExtractor.Extract(text));
        Assert.Equal(min, timer.MinSeconds);
        Assert.Equal(max, timer.MaxSeconds);
    }

    /// <summary>
    /// Text with no duration gives no timers.
    /// </summary>
    [Fact]
    public void Extract_NoDuration_ReturnsEmpty()
    {
        Assert.Empty(TimerExtractor.Extract("Boil a half-full kettle with 2 tbsp salt."));
    }
}