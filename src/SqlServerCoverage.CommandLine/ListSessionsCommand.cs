using Spectre.Cli;
using Spectre.Console;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ValidationResult = Spectre.Cli.ValidationResult;

namespace SqlServerCoverage.CommandLine
{
    internal sealed class ListSessionsCommand : Command<ListSessionsCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("The connection string to the SQLServer instance.")]
            [CommandOption("--connection-string")]
            public string ConnectionString { get; init; }

            public override ValidationResult Validate()
            {
                return string.IsNullOrEmpty(ConnectionString)
                    ? ValidationResult.Error("ConnectionString is mandatory")
                    : ValidationResult.Success();
            }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var controller = CodeCoverage.NewController(settings.ConnectionString, "master");
            var sessions = controller.ListSessions();
            if (sessions.Count == 0) AnsiConsole.Write("No sessions found");
            foreach (var session in sessions)
                AnsiConsole.WriteLine(session);
            return 0;
        }
    }
}