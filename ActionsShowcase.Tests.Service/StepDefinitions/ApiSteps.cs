using ActionsShowcase.Tests.Service.Infrastructure;
using Reqnroll;
using System.Net;
using System.Text.Json;

namespace ActionsShowcase.Tests.Service.StepDefinitions;

[Binding]
public sealed class ApiSteps
{
    private const string ResponseKey = "response";
    private const string BodyKey = "body";
    private const string ArrayKey = "array";

    private readonly ScenarioContext _scenarioContext;

    public ApiSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [When(@"I send a GET request to ""(.*)""")]
    public async Task WhenISendAGetRequest(string path)
    {
        var response = await TestEnvironment.Client.GetAsync(path);
        _scenarioContext[ResponseKey] = response;
        _scenarioContext[BodyKey] = await response.Content.ReadAsStringAsync();
    }

    [Then(@"the response status code is (\d+)")]
    public void ThenTheResponseStatusCodeIs(int statusCode)
    {
        var response = (HttpResponseMessage)_scenarioContext[ResponseKey];
        Assert.Equal((HttpStatusCode)statusCode, response.StatusCode);
    }

    [Then(@"the response is a non-empty JSON string array")]
    public void ThenTheResponseIsANonEmptyJsonStringArray()
    {
        var body = (string)_scenarioContext[BodyKey];
        var array = JsonSerializer.Deserialize<string[]>(body);

        Assert.NotNull(array);
        Assert.NotEmpty(array!);

        _scenarioContext[ArrayKey] = array!;
    }

    [Then(@"the JSON array has between (\d+) and (\d+) items")]
    public void ThenTheJsonArrayHasBetweenAndItems(int min, int max)
    {
        var array = (string[])_scenarioContext[ArrayKey];
        Assert.InRange(array.Length, min, max);
    }

    [Then(@"each string in the JSON array has length between (\d+) and (\d+)")]
    public void ThenEachStringInTheJsonArrayHasLengthBetweenAnd(int min, int max)
    {
        var array = (string[])_scenarioContext[ArrayKey];
        Assert.All(array, s => Assert.InRange(s.Length, min, max));
    }
}
