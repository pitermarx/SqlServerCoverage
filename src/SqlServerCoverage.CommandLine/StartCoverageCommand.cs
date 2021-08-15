using Spectre.Console.Cli;
using Spectre.Console;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SqlServerCoverage.CommandLine
{
    internal sealed class StartCoverageCommand : Command<StartCoverageCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("The connection string to the SQLServer instance.")]
            [CommandOption("--connection-string")]
            public string ConnectionString { get; init; }

            [Description("The name of the database to trace coverage events.")]
            [CommandOption("--database")]
            public string Database { get; init; }

            public override ValidationResult Validate()
            {
                if (string.IsNullOrEmpty(ConnectionString))
                {
                    return ValidationResult.Error("--connection-string is mandatory");
                }

                if (string.IsNullOrEmpty(Database))
                {
                    return ValidationResult.Error("--database is mandatory");
                }

                return ValidationResult.Success();
            }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var controller = CodeCoverage.NewController(settings.ConnectionString, settings.Database);
            var session = controller.NewSession();
            AnsiConsole.Write(session.SessionName);
            return 0;
        }
    }
}