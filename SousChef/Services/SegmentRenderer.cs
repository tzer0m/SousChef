using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using SousChef.Core.Cooking;

namespace SousChef.Services;

/// <summary>
/// Renders annotated instruction segments as HTML without stray whitespace between them.
/// </summary>
public static class SegmentRenderer
{
    /// <summary>
    /// Renders segments as inline HTML.
    /// </summary>
    /// <param name="segments">The segments to render.</param>
    /// <param name="timerLabel">The label for timers started from this text.</param>
    /// <param name="interactive">Whether timers are buttons that start a countdown.</param>
    public static IHtmlContent Render(IEnumerable<TextSegment> segments, string timerLabel, bool interactive)
    {
        HtmlEncoder encoder = HtmlEncoder.Default;
        StringBuilder html = new();
        foreach (TextSegment segment in segments)
        {
            string text = encoder.Encode(segment.Text);
            if (segment.Kind == SegmentKind.Amount)
            {
                html.Append(CultureInfo.InvariantCulture, $"<span class=\"sc-amount\">{text}</span>");
            }
            else if (segment.Timer != null && interactive)
            {
                html.Append(CultureInfo.InvariantCulture, $"<button type=\"button\" class=\"sc-timer-chip\" data-label=\"{encoder.Encode(timerLabel)}\" data-min=\"{segment.Timer.MinSeconds}\" data-max=\"{segment.Timer.MaxSeconds}\">⏱ {text}</button>");
            }
            else if (segment.Timer != null)
            {
                html.Append(CultureInfo.InvariantCulture, $"<span class=\"sc-timer-text\">⏱ {text}</span>");
            }
            else if (segment.IsIngredient || segment.IsHot)
            {
                string classes = segment.IsIngredient && segment.IsHot ? "sc-ingredient sc-hot" : segment.IsIngredient ? "sc-ingredient" : "sc-hot";
                html.Append(CultureInfo.InvariantCulture, $"<span class=\"{classes}\">{text}</span>");
            }
            else
            {
                html.Append(text);
            }
        }
        return new HtmlString(html.ToString());
    }
}