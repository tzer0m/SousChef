using System.Globalization;
using SousChef.Core.Cooking;
using SousChef.Core.Models;

namespace SousChef.Services;

/// <summary>
/// Works out how many people to cook for, remembering the last choice in a cookie.
/// </summary>
public static class ServingsPreference
{
    private const string CookieName = "servings";

    /// <summary>
    /// Returns the requested serving count, or the remembered one, or the card's smallest size.
    /// </summary>
    /// <param name="requested">The serving count in the URL, if any.</param>
    /// <param name="context">The HTTP context.</param>
    /// <param name="recipe">The recipe being viewed.</param>
    public static int Resolve(int? requested, HttpContext context, Recipe recipe)
    {
        int fallback = recipe.ServingSizes.Count > 0 ? recipe.ServingSizes.Min() : 2;
        int servings = requested ?? (int.TryParse(context.Request.Cookies[CookieName], CultureInfo.InvariantCulture, out int saved) ? saved : fallback);
        servings = Math.Clamp(servings, ServingScaler.MinServings, ServingScaler.MaxServings);
        if (requested.HasValue)
        {
            context.Response.Cookies.Append(CookieName, servings.ToString(CultureInfo.InvariantCulture), new CookieOptions { MaxAge = TimeSpan.FromDays(365), IsEssential = true, SameSite = SameSiteMode.Lax });
        }
        return servings;
    }
}