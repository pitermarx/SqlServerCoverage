using Spectre.Cli;
using Spectre.Console;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ValidationResult = Spectre.Cli.ValidationResult;

namespace SqlServerCoverage.CommandLine
{
    internal sealed class StopCoverageCommand : Command<StopCoverageCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("The connection string to the SQLServer instance.")]
            [CommandOption("--connection-string")]
            public string ConnectionString { get; init; }

            [Description("The name of session started with the start command.")]
            [CommandOption("--id")]
            public string Id { get; init; }

            public override ValidationResult Validate()
            {
                if (string.IsNullOrEmpty(ConnectionString))
                {
                    return ValidationResult.Error("--connection-string is mandatory");
                }

                if (string.IsNullOrEmpty(Id))
                {
                    return ValidationResult.Error("--id is mandatory");
                }

                return ValidationResult.Success();
            }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var controller = CodeCoverage.NewController(settings.ConnectionString, "master", settings.Id);
            var session = controller.AttachSession();
            session.StopSession();
            AnsiConsole.MarkupLine($"Session {settings.Id} stopped");
            return 0;
        }
    }
}