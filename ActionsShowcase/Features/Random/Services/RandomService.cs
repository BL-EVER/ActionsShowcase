using ActionsShowcase.Options;
using Microsoft.Extensions.Options;

namespace ActionsShowcase.Features.Random.Services;

public class RandomService : IRandomService
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private readonly IOptionsMonitor<RandomOptions> _options;

    public RandomService(IOptionsMonitor<RandomOptions> options)
    {
        _options = options;
    }

    public string[] GenerateStrings()
    {
        var opts = _options.CurrentValue;
        var rng = System.Random.Shared;

        var count = rng.Next(opts.MinCount, opts.MaxCount + 1);
        var result = new string[count];
        for (var i = 0; i < count; i++)
        {
            var length = rng.Next(opts.MinLength, opts.MaxLength + 1);
            var chars = new char[length];
            for (var j = 0; j < length; j++)
            {
                chars[j] = Alphabet[rng.Next(Alphabet.Length)];
            }
            result[i] = new string(chars);
        }
        return result;
    }
}
