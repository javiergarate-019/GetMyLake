namespace GetMyLake.Core.Matching;

public sealed record RotationSearchOptions
{
    public double CoarseStepDegrees { get; init; } = 5;

    public double FineStepDegrees { get; init; } = 0.25;

    internal void Validate()
    {
        if (CoarseStepDegrees is <= 0 or > 360)
        {
            throw new ArgumentOutOfRangeException(nameof(CoarseStepDegrees));
        }

        if (FineStepDegrees <= 0 || FineStepDegrees > CoarseStepDegrees)
        {
            throw new ArgumentOutOfRangeException(nameof(FineStepDegrees));
        }
    }
}
