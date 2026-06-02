using ActionsShowcase.Features.Random;
using Microsoft.AspNetCore.Mvc;

namespace ActionsShowcase.Controllers;

[ApiController]
[Route("[controller]")]
public class RandomController : ControllerBase
{
    private readonly IRandomService _randomService;

    public RandomController(IRandomService randomService)
    {
        _randomService = randomService;
    }

    [HttpGet(Name = "Random")]
    public string[] Get()
    {
        return _randomService.GenerateStrings();
    }
}
