using ActionsShowcase.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ActionsShowcase.Controllers;


[ApiController]
[Route("[controller]")]
public class SecretController : ControllerBase
{
    private readonly IOptionsMonitor<SecretsOptions> _options;

    public SecretController(IOptionsMonitor<SecretsOptions> options)
    {
        _options = options;
    }

    [HttpGet(Name = "Secrets")]
    public string[] Get()
    {
        return _options.CurrentValue.Values;
    }
}
