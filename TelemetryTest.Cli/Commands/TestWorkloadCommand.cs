using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Octokit;

namespace TelemetryTest.Cli.Commands;

[Command("github-info")]
public class GitHubInfoCommand : BaseAsyncCommand
{
    [Option("--org")]
    public string Organization { get; set; } = "dotnet";
    
    public GitHubInfoCommand(IConsole console, ILogger logger) : base(console, logger)
    {
    }

    public override async Task OnExecuteAsync(CommandLineApplication app, CancellationToken cancellationToken)
    {
        var client = new GitHubClient(new ProductHeaderValue("TestApp"));

        var repos = await client.Repository.GetAllForOrg(Organization, new ApiOptions {PageCount = 20});

        foreach (Repository repo in repos)
        {
            // client.Repository.PullRequest.GetAllForRepository()
        }
    }
}