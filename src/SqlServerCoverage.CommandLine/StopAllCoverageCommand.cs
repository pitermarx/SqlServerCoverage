﻿namespace SqlServerCoverage.CommandLine;

internal sealed class StopAllCoverageCommand : Command<StopAllCoverageCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("The connection string to the SQLServer instance.")]
        [CommandOption("--connection-string")]
        public string? ConnectionString { get; init; }

        [Description("Only stop sessions of missing dbs.")]
        [CommandOption("--only-missing-dbs")]
        public bool OnlyMissingDbs { get; init; }

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
        var controller = new CoverageSessionController(settings.ConnectionString!);
        foreach (var (id, db) in controller.ListSessions())
        {
            if (db is not null && settings.OnlyMissingDbs)
                continue;

            new CoverageSessionController(settings.ConnectionString!).StopSession(id);
            AnsiConsole.MarkupLine($"Session {id} stopped");
        }
        return 0;
    }
}
