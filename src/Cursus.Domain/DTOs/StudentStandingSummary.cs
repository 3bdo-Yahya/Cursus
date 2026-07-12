namespace Cursus.Domain.DTOs;

/// <summary>Aggregate academic-standing counts for admin overview surfaces.</summary>
public sealed record StudentStandingSummary(
    int Total,
    int Good,
    int WarningOrProbation,
    int Dismissed);
