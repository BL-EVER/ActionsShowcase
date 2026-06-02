using ActionsShowcase.Options;

namespace ActionsShowcase.Tests.Unit.Options;

public class RandomOptionsTests
{
    [Fact]
    public void SectionName_IsRandom()
    {
        Assert.Equal("Random", RandomOptions.SectionName);
    }

    [Fact]
    public void Defaults_AreSet()
    {
        var opts = new RandomOptions();

        Assert.Equal(2, opts.MinCount);
        Assert.Equal(10, opts.MaxCount);
        Assert.Equal(2, opts.MinLength);
        Assert.Equal(10, opts.MaxLength);
    }

    [Fact]
    public void Properties_CanBeAssigned()
    {
        var opts = new RandomOptions
        {
            MinCount = 1,
            MaxCount = 3,
            MinLength = 5,
            MaxLength = 7
        };

        Assert.Equal(1, opts.MinCount);
        Assert.Equal(3, opts.MaxCount);
        Assert.Equal(5, opts.MinLength);
        Assert.Equal(7, opts.MaxLength);
    }
}
