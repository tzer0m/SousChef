using System.Text;
using System.Text.RegularExpressions;
using SousChef.Core.Models;

namespace SousChef.Core.Cooking;

/// <summary>
/// Turns sub-steps, annotated in order, into segments with scaled amounts, highlighted ingredients and timers.
/// </summary>
/// <param name="recipe">The recipe being cooked.</param>
/// <param name="servings">The number of people.</param>
public partial class StepAnnotator(Recipe recipe, int servings)
{
    private readonly HashSet<int> _used = [];

    /// <summary>
    /// Annotates every step of the recipe in order.
    /// </summary>
    public List<AnnotatedStep> AnnotateAll()
    {
        return [.. recipe.Steps.Select(s => new AnnotatedStep { Step = s, SubSteps = [.. s.SubSteps.Select(sub => new AnnotatedSubStep { SubStep = sub, Segments = Annotate(sub) })] })];
    }

    /// <summary>
    /// Annotates the next sub-step.
    /// </summary>
    /// <param name="subStep">The sub-step.</param>
    public List<TextSegment> Annotate(SubStep subStep)
    {
        string text = subStep.Text;
        int length = text.Length;
        bool[] removed = new bool[length];
        bool[] hot = new bool[length];
        bool[] ingredient = new bool[length];
        int[] timer = [.. Enumerable.Repeat(-1, length)];
        Match see = SeeAmountRegex().Match(text);
        if (see.Success)
        {
            Array.Fill(removed, true, see.Index, see.Length);
        }
        foreach (TextRange range in subStep.HotRanges.Where(r => r.End <= length))
        {
            Array.Fill(hot, true, range.Start, range.Length);
        }
        for (int i = 0; i < subStep.Timers.Count; i++)
        {
            StepTimer stepTimer = subStep.Timers[i];
            if (stepTimer.End <= length)
            {
                Array.Fill(timer, i, stepTimer.Start, stepTimer.Length);
            }
        }
        Dictionary<int, string> amounts = [];
        foreach (IngredientMention mention in IngredientMatcher.FindMentions(text, recipe.Ingredients))
        {
            Array.Fill(ingredient, true, mention.Start, mention.Length);
            Ingredient item = recipe.Ingredients[mention.IngredientIndex];
            bool firstUse = _used.Add(mention.IngredientIndex);
            bool pointedTo = see.Success && see.Groups["table"].Value.Equals("pantry", StringComparison.OrdinalIgnoreCase) == item.IsPantry;
            if ((firstUse || pointedTo) && !amounts.ContainsKey(mention.End))
            {
                string amount = QuantityFormatter.FormatAmount(ServingScaler.Scale(item, servings), item.Unit);
                if (amount.Length > 0)
                {
                    amounts[mention.End] = amount;
                }
            }
        }
        List<TextSegment> segments = [];
        StringBuilder buffer = new();
        (bool Hot, bool Ingredient, int Timer) state = (false, false, -1);
        for (int i = 0; i < length; i++)
        {
            if (removed[i])
            {
                continue;
            }
            (bool Hot, bool Ingredient, int Timer) next = (hot[i], ingredient[i], timer[i]);
            if (next != state)
            {
                Flush(segments, buffer, state, subStep);
                state = next;
            }
            buffer.Append(text[i]);
            if (amounts.TryGetValue(i + 1, out string? amount))
            {
                Flush(segments, buffer, state, subStep);
                segments.Add(new TextSegment { Kind = SegmentKind.Amount, Text = amount });
            }
        }
        Flush(segments, buffer, state, subStep);
        return segments;
    }

    /// <summary>
    /// Adds the buffered text as a segment and clears the buffer.
    /// </summary>
    /// <param name="segments">The segments to add to.</param>
    /// <param name="buffer">The buffered text.</param>
    /// <param name="state">The formatting of the buffered text.</param>
    /// <param name="subStep">The sub-step, for looking up timers.</param>
    private static void Flush(List<TextSegment> segments, StringBuilder buffer, (bool Hot, bool Ingredient, int Timer) state, SubStep subStep)
    {
        if (buffer.Length == 0)
        {
            return;
        }
        segments.Add(new TextSegment { Kind = SegmentKind.Text, Text = buffer.ToString(), IsHot = state.Hot, IsIngredient = state.Ingredient, Timer = state.Timer >= 0 ? subStep.Timers[state.Timer] : null });
        buffer.Clear();
    }

    [GeneratedRegex(@"\s*\(see (?<table>ingredients|pantry) for amounts?\)", RegexOptions.IgnoreCase)]
    private static partial Regex SeeAmountRegex();
}