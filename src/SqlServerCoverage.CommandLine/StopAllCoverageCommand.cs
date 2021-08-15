using Spectre.Console.Cli;
using Spectre.Console;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SqlServerCoverage.CommandLine
{
    internal sealed class StopAllCoverageCommand : Command<StopAllCoverageCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("The connection string to the SQLServer instance.")]
            [CommandOption("--connection-string")]
            public string ConnectionString { get; init; }

            public override ValidationResult Validate()
            {
                if (string.IsNullOrEmpty(ConnectionString))
                {
                    return ValidationResult.Error("--connection-string is mandatory");
                }

                return ValidationResult.Success();
            }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var controller = CodeCoverage.NewController(settings.ConnectionString, "master");
            foreach (var id in controller.ListSessions())
            {
                CodeCoverage
                    .NewController(settings.ConnectionString, "master", id)
                    .AttachSession()
                    .StopSession();
                AnsiConsole.MarkupLine($"Session {id} stopped");
            }
            return 0;
        }
    }
}