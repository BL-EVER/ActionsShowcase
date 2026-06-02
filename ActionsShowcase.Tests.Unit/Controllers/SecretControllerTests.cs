using ActionsShowcase.Controllers;
using ActionsShowcase.Options;

namespace ActionsShowcase.Tests.Unit.Controllers;

public class SecretControllerTests
{
    [Fact]
    public void Get_ReturnsConfiguredValues()
    {
        var values = new[] { "a", "b", "c" };
        var monitor = new TestOptionsMonitor<SecretsOptions>(new SecretsOptions { Values = values });
        var controller = new SecretController(monitor);

        var result = controller.Get();

        Assert.Same(values, result);
    }

    [Fact]
    public void Get_ReturnsEmpty_WhenNoValuesConfigured()
    {
        var monitor = new TestOptionsMonitor<SecretsOptions>(new SecretsOptions());
        var controller = new SecretController(monitor);

        var result = controller.Get();

        Assert.Empty(result);
    }
}
