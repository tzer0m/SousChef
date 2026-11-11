namespace SousChef.Core.Parsing;

/// <summary>
/// An OCR letter in upright page coordinates.
/// </summary>
/// <param name="Value">The letter text.</param>
/// <param name="X0">The left edge.</param>
/// <param name="X1">The right edge.</param>
/// <param name="Y">The baseline, corrected for any tilt in the scan.</param>
/// <param name="RawY">The baseline as scanned, used to find the letter on the image.</param>
/// <param name="Height">The letter height.</param>
internal record ScanLetter(string Value, double X0, double X1, double Y, double RawY, double Height);