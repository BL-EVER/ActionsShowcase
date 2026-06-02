namespace ActionsShowcase.Options;

public class RandomOptions
{
    public const string SectionName = "Random";

    public int MinCount { get; set; } = 2;
    public int MaxCount { get; set; } = 10;
    public int MinLength { get; set; } = 2;
    public int MaxLength { get; set; } = 10;
}
