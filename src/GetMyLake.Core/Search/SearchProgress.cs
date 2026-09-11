namespace GetMyLake.Core.Search;

public sealed record SearchProgress(
    string Stage,
    int Processed,
    int Total,
    int RetainedCandidates,
    int Errors);
