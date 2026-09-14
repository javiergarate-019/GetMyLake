namespace GetMyLake.Core.Visualization;

public sealed record ComparisonRenderOptions
{
    public int Width { get; init; } = 1800;

    public int Height { get; init; } = 800;

    internal void Validate()
    {
        if (Width < 900)
        {
            throw new ArgumentOutOfRangeException(nameof(Width), "Width must be at least 900 pixels.");
        }

        if (Height < 500)
        {
            throw new ArgumentOutOfRangeException(nameof(Height), "Height must be at least 500 pixels.");
        }
    }
}
