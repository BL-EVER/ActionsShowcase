using ActionsShowcase.Options;

namespace ActionsShowcase.Tests.Unit.Options;

public class SecretsOptionsTests
{
    [Fact]
    public void SectionName_IsSecrets()
    {
        Assert.Equal("Secrets", SecretsOptions.SectionName);
    }

    [Fact]
    public void Values_DefaultsToEmpty()
    {
        var opts = new SecretsOptions();

        Assert.NotNull(opts.Values);
        Assert.Empty(opts.Values);
    }

    [Fact]
    public void Values_CanBeAssigned()
    {
        var values = new[] { "a", "b" };
        var opts = new SecretsOptions { Values = values };

        Assert.Same(values, opts.Values);
    }
}
