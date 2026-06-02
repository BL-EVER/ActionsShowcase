using ActionsShowcase.Controllers;
using ActionsShowcase.Features.Random;

namespace ActionsShowcase.Tests.Unit.Controllers;

public class RandomControllerTests
{
    [Fact]
    public void Get_DelegatesToService()
    {
        var expected = new[] { "abc", "def" };
        var service = new StubRandomService(expected);
        var controller = new RandomController(service);

        var result = controller.Get();

        Assert.Same(expected, result);
    }

    private sealed class StubRandomService : IRandomService
    {
        private readonly string[] _result;

        public StubRandomService(string[] result) => _result = result;

        public string[] GenerateStrings() => _result;
    }
}
