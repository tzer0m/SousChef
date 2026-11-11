namespace SousChef.Tests;

/// <summary>
/// Finds the sample recipe cards in the solution's samples folder (not committed to git).
/// </summary>
internal static class TestSamples
{
    /// <summary>
    /// The file name of the Weeknight Chicken Fried Rice card.
    /// </summary>
    public const string ChickenFriedRice = "2026-11-11 HelloFresh 59 Weeknight Chicken Fried Rice.pdf";

    /// <summary>
    /// The file name of the scanned Quick Butter Chicken Masala card.
    /// </summary>
    public const string ButterChicken = "2026-11-11 HelloFresh 1 Quick Butter Chicken Masala.pdf";

    /// <summary>
    /// Returns the samples folder, or null if it can't be found.
    /// </summary>
    public static string? FindFolder()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, "samples");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            directory = directory.Parent;
        }
        return null;
    }

    /// <summary>
    /// Returns the full path of a sample card, or null if it isn't present.
    /// </summary>
    /// <param name="fileName">The card's file name.</param>
    public static string? Find(string fileName)
    {
        string? folder = FindFolder();
        string? path = folder == null ? null : Path.Combine(folder, fileName);
        return path != null && File.Exists(path) ? path : null;
    }

    /// <summary>
    /// Returns every sample card as test data, or a single null row when there are none.
    /// </summary>
    public static IEnumerable<object?[]> All()
    {
        string? folder = FindFolder();
        List<string> files = folder == null ? [] : [.. Directory.GetFiles(folder, "*.pdf").Order()];
        if (files.Count == 0)
        {
            return [[null]];
        }
        return files.Select(f => new object?[] { Path.GetFileName(f) });
    }
}