using System.Diagnostics;
using ActionsShowcase.Tests.Service.Infrastructure;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Reqnroll;

namespace ActionsShowcase.Tests.Service.Hooks;

[Binding]
public static class ContainerHooks
{
    private const int AppPort = 8081;
    private const string ImageRepository = "actionsshowcase";
    private const string ImageTag = "service-tests";

    private static IContainer? _container;

    [BeforeTestRun]
    public static async Task StartAsync()
    {
        await PublishImageAsync();

        _container = new ContainerBuilder()
            .WithImage($"{ImageRepository}:{ImageTag}")
            .WithPortBinding(AppPort, assignRandomHostPort: true)
            .WithEnvironment("ASPNETCORE_URLS", $"http://+:{AppPort}")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r.ForPort(AppPort).ForPath("/Secret")))
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        var baseUrl = $"http://{_container.Hostname}:{_container.GetMappedPublicPort(AppPort)}/";
        TestEnvironment.Client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    [AfterTestRun]
    public static async Task StopAsync()
    {
        TestEnvironment.Client?.Dispose();

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    private static async Task PublishImageAsync()
    {
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "ActionsShowcase", "ActionsShowcase.csproj");

        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("publish");
        psi.ArgumentList.Add(projectPath);
        psi.ArgumentList.Add("-t:PublishContainer");
        psi.ArgumentList.Add($"/p:ContainerRepository={ImageRepository}");
        psi.ArgumentList.Add($"/p:ContainerImageTag={ImageTag}");
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("Release");
        psi.ArgumentList.Add("--nologo");
        psi.ArgumentList.Add("--verbosity");
        psi.ArgumentList.Add("quiet");

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start 'dotnet publish'.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            throw new InvalidOperationException(
                $"'dotnet publish' failed with exit code {process.ExitCode}.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }
    }

    private static string FindSolutionDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ActionsShowcase", "ActionsShowcase.csproj")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            "Could not locate the ActionsShowcase project from the test assembly directory.");
    }
}
