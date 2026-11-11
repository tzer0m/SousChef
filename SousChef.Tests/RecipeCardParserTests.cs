using SousChef.Core.Models;
using SousChef.Core.Parsing;

namespace SousChef.Tests;

/// <summary>
/// Tests for parsing recipe card PDFs.
/// </summary>
public class RecipeCardParserTests
{
    /// <summary>
    /// Every sample card has the essentials; downloaded cards must also parse without warnings (scans may flag OCR misreads).
    /// </summary>
    /// <param name="fileName">The sample card's file name, or null when there are no samples.</param>
    [Theory]
    [MemberData(nameof(TestSamples.All), MemberType = typeof(TestSamples))]
    public void Parse_EverySample_HasEssentials(string? fileName)
    {
        if (fileName == null)
        {
            return;
        }
        string path = TestSamples.Find(fileName)!;
        ParseResult result = new RecipeCardParser().Parse(File.ReadAllBytes(path), fileName);
        Recipe recipe = result.Recipe;
        Assert.False(string.IsNullOrWhiteSpace(recipe.Title));
        Assert.NotNull(recipe.Number);
        Assert.NotNull(recipe.TimeMaxMinutes);
        Assert.NotEmpty(recipe.ServingSizes);
        Assert.True(recipe.Ingredients.Count >= 5, $"Only {recipe.Ingredients.Count} ingredients found.");
        Assert.InRange(recipe.Steps.Count, 4, 6);
        Assert.All(recipe.Steps, s => Assert.NotEmpty(s.SubSteps));
        Assert.All(recipe.Steps, s => Assert.False(string.IsNullOrWhiteSpace(s.Title)));
        Assert.DoesNotContain(recipe.Steps.SelectMany(s => s.SubSteps), s => s.Text.Contains('\0') || s.Text.EndsWith("Enjoy!"));
        Assert.NotNull(recipe.HeroImageKey);
        Assert.Equal(recipe.Steps.Count, recipe.Steps.Count(s => s.ImageKey != null));
        if (!recipe.IsScan)
        {
            Assert.Empty(recipe.ParseWarnings);
            Assert.All(recipe.Ingredients, i => Assert.Equal(recipe.ServingSizes, [.. i.Quantities.Keys.Order()]));
        }
    }

    /// <summary>
    /// The scanned butter chicken card is read from its OCR layer, with titles, timers, notes, hot text and pantry quantities.
    /// </summary>
    [Fact]
    public void Parse_ButterChickenScan_ReadsLayout()
    {
        Recipe? recipe = ParseSample(TestSamples.ButterChicken)?.Recipe;
        if (recipe == null)
        {
            return;
        }
        Assert.True(recipe.IsScan);
        Assert.Equal(1, recipe.Number);
        Assert.Equal("Quick Butter Chicken Masala", recipe.Title);
        Assert.Equal("2026-W46", recipe.WeekCode);
        Assert.Equal(20, recipe.TimeMinMinutes);
        Assert.Equal(25, recipe.TimeMaxMinutes);
        Assert.Equal(["Cook the Garlic Rice", "Fry the Chicken", "Simmer and Spice", "Bring on the Beans", "Finish the Curry", "Serve"], recipe.Steps.Select(s => s.Title));
        SubStep fry = recipe.Steps[1].SubSteps[1];
        Assert.Equal(300, Assert.Single(fry.Timers).MinSeconds);
        Assert.StartsWith("Notice a stronger smell", fry.Tip);
        Assert.NotNull(fry.Important);
        Assert.True(recipe.Ingredients.Single(i => i.Name == "North Indian Style Spice Mix").IsHot);
        Ingredient butter = recipe.Ingredients.Single(i => i.Name == "Butter");
        Assert.True(butter.IsPantry);
        Assert.Equal(30, butter.Quantities[3]);
        Assert.Equal(1.5m, recipe.Ingredients.Single(i => i.Name == "Mustard Seeds").Quantities[3]);
    }

    /// <summary>
    /// The chicken fried rice card's header, tags and time are read correctly.
    /// </summary>
    [Fact]
    public void Parse_ChickenFriedRice_ReadsFrontPage()
    {
        Recipe? recipe = ParseSample(TestSamples.ChickenFriedRice)?.Recipe;
        if (recipe == null)
        {
            return;
        }
        Assert.Equal(59, recipe.Number);
        Assert.Null(recipe.ProductCode);
        Assert.False(recipe.IsScan);
        Assert.Equal("2026-W46", recipe.WeekCode);
        Assert.Equal("Weeknight Chicken Fried Rice", recipe.Title);
        Assert.Equal("with Sambal Mayo, Mushrooms and Pak Choi", recipe.Subtitle);
        Assert.Equal(["Calorie Smart", "Very Hot", "High Protein"], recipe.Tags);
        Assert.Equal("Very Hot", recipe.SpiceLevel);
        Assert.Equal(20, recipe.TimeMinMinutes);
        Assert.Equal(25, recipe.TimeMaxMinutes);
        Assert.Equal(["1 of your 5 a day"], recipe.Extras);
    }

    /// <summary>
    /// The chicken fried rice card's tables are read correctly, including allergen codes inside the name cell.
    /// </summary>
    [Fact]
    public void Parse_ChickenFriedRice_ReadsSidebar()
    {
        Recipe? recipe = ParseSample(TestSamples.ChickenFriedRice)?.Recipe;
        if (recipe == null)
        {
            return;
        }
        Assert.Equal(["Bowl", "Garlic Press", "Kettle", "Large Frying Pan", "Large Saucepan", "Sieve"], recipe.Tools);
        Assert.Equal([2, 3, 4], recipe.ServingSizes);
        Assert.Equal(13, recipe.Ingredients.Count);
        Ingredient soy = recipe.Ingredients.Single(i => i.Name == "Soy Sauce");
        Assert.Equal("ml", soy.Unit);
        Assert.Equal([13, 11, 15], soy.AllergenCodes);
        Assert.Equal(10, soy.Quantities[2]);
        Assert.Equal(20, soy.Quantities[3]);
        Ingredient spice = recipe.Ingredients.Single(i => i.Name == "Indonesian Style Spice Mix");
        Assert.Equal("sachet(s)", spice.Unit);
        Assert.Equal(2, spice.Quantities[4]);
        Assert.True(spice.IsHot);
        Ingredient lime = recipe.Ingredients.Single(i => i.Name == "Lime");
        Assert.Equal(0.5m, lime.Quantities[2]);
        Assert.True(lime.StoreInFridge);
        Ingredient honey = recipe.Ingredients.Single(i => i.Name == "Honey");
        Assert.True(honey.IsPantry);
        Assert.True(honey.NotIncluded);
        Assert.Equal("tbsp", honey.Unit);
        Assert.Equal(1.5m, honey.Quantities[3]);
        Assert.False(recipe.Ingredients.Single(i => i.Name == "Ketjap Manis").IsPantry);
        Assert.Equal(["Oil", "Salt", "Pepper", "Water"], recipe.ImpliedPantry);
        Assert.Equal(9, recipe.Nutrition.Count);
        Assert.Equal("2467/590", recipe.Nutrition.Single(n => n.Name == "Energy").PerServing);
        Assert.Equal(["Soya", "Cereals containing gluten", "Wheat"], recipe.Allergens.Select(a => a.Name));
    }

    /// <summary>
    /// The chicken fried rice card's steps are read in the right order with tips, warnings, timers and hot text.
    /// </summary>
    [Fact]
    public void Parse_ChickenFriedRice_ReadsSteps()
    {
        ParseResult? result = ParseSample(TestSamples.ChickenFriedRice);
        if (result == null)
        {
            return;
        }
        Recipe recipe = result.Recipe;
        Assert.Equal(["Cook the Rice", "Brown the Chicken", "Prep Time", "Bring on the Flavour", "Combine and Stir", "Finish and Serve"], recipe.Steps.Select(s => s.Title));
        Assert.Equal([3, 3, 4, 2, 4, 3], recipe.Steps.Select(s => s.SubSteps.Count));
        SubStep rice = recipe.Steps[0].SubSteps[1];
        Assert.Equal("Pour the boiled water into a large saucepan with ¼ tsp salt on high heat. Add the rice and cook for 10-12 mins.", rice.Text);
        StepTimer timer = Assert.Single(rice.Timers);
        Assert.Equal(600, timer.MinSeconds);
        Assert.Equal(720, timer.MaxSeconds);
        SubStep brown = recipe.Steps[1].SubSteps[2];
        Assert.Equal("Wash your hands and equipment after handling raw chicken and its packaging.", brown.Important);
        Assert.StartsWith("Notice a stronger smell", recipe.Steps[1].SubSteps[1].Tip);
        Assert.True(recipe.Steps[1].SubSteps[0].IsParallel);
        SubStep flavour = recipe.Steps[3].SubSteps[0];
        TextRange hot = Assert.Single(flavour.HotRanges);
        Assert.Equal("Indonesian style spice mix", flavour.Text.Substring(hot.Start, hot.Length));
        Assert.Equal(["step-1", "step-2", "step-3", "step-4", "step-5", "step-6"], recipe.Steps.Select(s => s.ImageKey));
        Assert.Equal(11, recipe.Ingredients.Count(i => i.ImageKey != null));
        Assert.All(result.Images, i => Assert.Equal("image/jpeg", i.ContentType));
    }

    /// <summary>
    /// Parses a sample card, or returns null if the sample isn't present.
    /// </summary>
    /// <param name="fileName">The card's file name.</param>
    private static ParseResult? ParseSample(string fileName)
    {
        string? path = TestSamples.Find(fileName);
        return path == null ? null : new RecipeCardParser().Parse(File.ReadAllBytes(path), fileName);
    }
}