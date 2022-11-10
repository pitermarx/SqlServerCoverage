namespace SqlServerCoverage.CommandLine;

internal sealed class StopCoverageCommand : Command<StopCoverageCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("The connection string to the SQLServer instance.")]
        [CommandOption("--connection-string")]
        public string? ConnectionString { get; init; }

        [Description("The name of session started with the start command.")]
        [CommandOption("--id")]
        public string? Id { get; init; }

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
        new CoverageSessionController(settings.ConnectionString!).StopSession(settings.Id!);
        AnsiConsole.MarkupLine($"Session {settings.Id} stopped");
        return 0;
    }
}
