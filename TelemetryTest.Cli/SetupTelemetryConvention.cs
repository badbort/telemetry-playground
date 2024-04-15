using McMaster.Extensions.CommandLineUtils.Conventions;

namespace TelemetryTest.Cli;

public class SetupTelemetryConvention : IConvention
{
    public void Apply(ConventionContext context)
    {
        // context.Application.Parse(new string[0]).SelectedCommand
    }
}