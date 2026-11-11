using System.ComponentModel.DataAnnotations;

namespace SousChef.Services;

/// <summary>
/// The orders the library can be sorted in.
/// </summary>
public enum LibrarySort
{
    /// <summary>
    /// Most recently imported first.
    /// </summary>
    [Display(Name = "Newest")]
    Newest,

    /// <summary>
    /// Alphabetical by title.
    /// </summary>
    [Display(Name = "A–Z")]
    Title,

    /// <summary>
    /// Shortest cooking time first.
    /// </summary>
    [Display(Name = "Quickest")]
    Quickest,

    /// <summary>
    /// Most often cooked first.
    /// </summary>
    [Display(Name = "Most cooked")]
    MostCooked
}