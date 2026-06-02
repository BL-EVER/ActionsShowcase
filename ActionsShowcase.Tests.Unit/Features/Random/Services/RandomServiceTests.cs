using ActionsShowcase.Features.Random.Services;
using ActionsShowcase.Options;

namespace ActionsShowcase.Tests.Unit.Features.Random.Services;

public class RandomServiceTests
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    [Fact]
    public void GenerateStrings_FixedConfig_ReturnsExactCountAndLength()
    {
        var monitor = new TestOptionsMonitor<RandomOptions>(new RandomOptions
        {
            MinCount = 5,
            MaxCount = 5,
            MinLength = 7,
            MaxLength = 7
        });
        var service = new RandomService(monitor);

        var result = service.GenerateStrings();

        Assert.Equal(5, result.Length);
        Assert.All(result, s => Assert.Equal(7, s.Length));
        Assert.All(result, s => Assert.All(s, c => Assert.Contains(c, Alphabet)));
    }

    [Fact]
    public void GenerateStrings_RangedConfig_StaysWithinBounds()
    {
        var monitor = new TestOptionsMonitor<RandomOptions>(new RandomOptions
        {
            MinCount = 2,
            MaxCount = 10,
            MinLength = 2,
            MaxLength = 10
        });
        var service = new RandomService(monitor);

        for (var i = 0; i < 20; i++)
        {
            var result = service.GenerateStrings();

            Assert.InRange(result.Length, 2, 10);
            Assert.All(result, s => Assert.InRange(s.Length, 2, 10));
            Assert.All(result, s => Assert.All(s, c => Assert.Contains(c, Alphabet)));
        }
    }
}
