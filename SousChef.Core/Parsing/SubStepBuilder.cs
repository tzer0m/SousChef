using System.Text;
using SousChef.Core.Cooking;
using SousChef.Core.Models;

namespace SousChef.Core.Parsing;

/// <summary>
/// Builds a sub-step from its words, splitting out tips, important notes and hot text.
/// </summary>
/// <param name="letter">The sub-step letter.</param>
internal class SubStepBuilder(string letter)
{
    private static readonly char[] TrailingPunctuation = [',', '.', ';', ':', ')', '!', '?'];

    private readonly StringBuilder _main = new();

    private readonly StringBuilder _tip = new();

    private readonly StringBuilder _important = new();

    private readonly List<TextRange> _hotRanges = [];

    private StringBuilder? _target;

    /// <summary>
    /// Adds the next word of the sub-step.
    /// </summary>
    /// <param name="word">The word.</param>
    /// <param name="startsLine">Whether the word starts a new line on the card.</param>
    public void Add(PdfWord word, bool startsLine)
    {
        if (word.Text.Equals("TIP:", StringComparison.OrdinalIgnoreCase))
        {
            _target = _tip;
            return;
        }
        if (word.Text.Equals("IMPORTANT:", StringComparison.OrdinalIgnoreCase))
        {
            _target = _important;
            return;
        }
        StringBuilder target = _target ?? _main;
        bool joinHyphen = startsLine && target.Length > 1 && target[^1] == '-' && char.IsLetter(target[^2]);
        if (target.Length > 0 && !joinHyphen)
        {
            target.Append(' ');
        }
        int start = target.Length;
        target.Append(word.Text);
        if (target == _main && word.Colour == TextColour.Red)
        {
            int length = word.Text.TrimEnd(TrailingPunctuation).Length;
            TextRange? previous = _hotRanges.Count > 0 ? _hotRanges[^1] : null;
            if (previous != null && start - previous.End <= 2 && _main.ToString(previous.End, start - previous.End).All(c => c == ' ' || TrailingPunctuation.Contains(c)))
            {
                previous.Length = start + length - previous.Start;
            }
            else if (length > 0)
            {
                _hotRanges.Add(new TextRange { Start = start, Length = length });
            }
        }
    }

    /// <summary>
    /// Creates the sub-step.
    /// </summary>
    public SubStep Build()
    {
        string text = _main.ToString().Trim();
        if (text.EndsWith("Enjoy!", StringComparison.OrdinalIgnoreCase))
        {
            text = text[..^"Enjoy!".Length].TrimEnd();
        }
        return new SubStep
        {
            Letter = letter,
            Text = text,
            HotRanges = _hotRanges,
            Timers = TimerExtractor.Extract(text),
            Tip = _tip.Length > 0 ? _tip.ToString().Trim() : null,
            Important = _important.Length > 0 ? _important.ToString().Trim() : null,
            IsParallel = text.StartsWith("Meanwhile", StringComparison.OrdinalIgnoreCase)
        };
    }
}