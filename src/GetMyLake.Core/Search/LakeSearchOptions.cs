namespace GetMyLake.Core.Search;

public sealed record LakeSearchOptions
{
    public int PrefilterCount { get; init; } = 500;

    public int TopCount { get; init; } = 20;

    public double SimplificationTolerance { get; init; } = 0.002;

    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    internal void Validate()
    {
        if (PrefilterCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(PrefilterCount));
        }

        if (TopCount <= 0 || TopCount > PrefilterCount)
        {
            throw new ArgumentOutOfRangeException(nameof(TopCount));
        }

        if (SimplificationTolerance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SimplificationTolerance));
        }

        if (MaxDegreeOfParallelism <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxDegreeOfParallelism));
        }
    }
}
