using SousChef.Core.Models;

namespace SousChef.Core.Parsing;

/// <summary>
/// Splits a step's lines into lettered sub-steps.
/// </summary>
internal static class SubStepReader
{
    /// <summary>
    /// Builds the sub-steps from a step's body lines, top to bottom.
    /// </summary>
    /// <param name="lines">The step's body lines.</param>
    public static List<SubStep> Read(List<List<PdfWord>> lines)
    {
        List<SubStep> subSteps = [];
        SubStepBuilder? current = null;
        foreach (List<PdfWord> line in lines)
        {
            for (int i = 0; i < line.Count; i++)
            {
                if (i == 0 && CardText.SubStepLetterRegex().IsMatch(line[i].Text))
                {
                    if (current != null)
                    {
                        subSteps.Add(current.Build());
                    }
                    current = new SubStepBuilder(line[i].Text[..1]);
                    continue;
                }
                current ??= new SubStepBuilder(string.Empty);
                current.Add(line[i], i == 0);
            }
        }
        if (current != null)
        {
            subSteps.Add(current.Build());
        }
        return [.. subSteps.Where(s => s.Text.Length > 0 || s.Tip != null || s.Important != null)];
    }
}