using Spectre.Cli;
using Spectre.Console;
using SqlServerCoverage.Data;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using ValidationResult = Spectre.Cli.ValidationResult;

namespace SqlServerCoverage.CommandLine
{
    internal sealed class CollectCoverageCommand : Command<CollectCoverageCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("The connection string to the SQLServer instance.")]
            [CommandOption("--connection-string")]
            public string ConnectionString { get; init; }

            [Description("The name of the database to trace coverage events.")]
            [CommandOption("--database")]
            public string Database { get; init; }

            [Description("The name of session started with the start command.")]
            [CommandOption("--id")]
            public string Id { get; init; }

            [Description("The directory the reports will be written to.")]
            [CommandOption("--output")]
            public string OutputDir { get; init; }

            [Description("Whether to write an HTML report.")]
            [CommandOption("--html")]
            public bool Html { get; init; }

            [Description("Whether to write an opencover report. Will also write source files.")]
            [CommandOption("--opencover")]
            public bool OpenCover { get; init; }

            [Description("Whether to write a summary to the console.")]
            [CommandOption("--summary")]
            public bool Summary { get; init; }

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

                if (string.IsNullOrEmpty(Id))
                {
                    return ValidationResult.Error("--id is mandatory");
                }

                if ((Html || OpenCover) && string.IsNullOrEmpty(OutputDir))
                {
                    return ValidationResult.Error("--output is mandatory when exporting to html or opencover xml formats");
                }

                if (!Html && !OpenCover && !Summary)
                {
                    return ValidationResult.Error("Please define --html --opencover or --summary");
                }

                return ValidationResult.Success();
            }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var controller = CodeCoverage.NewController(settings.ConnectionString, settings.Database, settings.Id);
            var session = controller.AttachSession();

            CoverageResult results = null;
            AnsiConsole.Status().Start("Collecting coverage data...", ctx =>
            {
                results = session.ReadCoverage();
            });

            if (settings.Summary)
            {
                RenderSummary(results);
            }

            if (settings.Html)
            {
                RenderHtml(settings, results);
            }

            if (settings.OpenCover)
            {
                RenderOpenCover(settings, results);
            }

            return 0;
        }

        private static void RenderOpenCover(Settings settings, CoverageResult results)
        {
            AnsiConsole.MarkupLine("Exporting OpenCover...");
            var sourceOutput = Path.Combine(settings.OutputDir, "source");
            if (!Directory.Exists(sourceOutput)) Directory.CreateDirectory(sourceOutput);

            var dir = Path.Combine(settings.OutputDir, $"{settings.Database}Coverage.xml");
            using var writer = File.CreateText(dir);
            results.OpenCoverXml(writer);

            foreach (var obj in results.SourceObjects)
            {
                File.WriteAllText(Path.Combine(sourceOutput, $"{obj.Name}.sql"), obj.Text);
            }
        }

        private static void RenderHtml(Settings settings, CoverageResult results)
        {
            AnsiConsole.MarkupLine("Exporting HTML...");
            if (!Directory.Exists(settings.OutputDir)) Directory.CreateDirectory(settings.OutputDir);

            var dir = Path.Combine(settings.OutputDir, $"{settings.Database}Coverage.html");
            using var writer = File.CreateText(dir);
            results.Html(writer);
        }

        private static void RenderSummary(CoverageResult results)
        {
            AnsiConsole.Render(new Rule("Coverage Summary").Alignment(Justify.Left));

            AnsiConsole.Render(new BarChart()
                .Width(results.StatementCount)
                .AddItem("Statements", results.StatementCount)
                .AddItem("Covered statements", results.CoveredStatementCount));

            var table = new Table();
            table.AddColumns("Type", "Name", "Coverable lines", "Covered lines", "Coverage");

            AnsiConsole.Live(table)
                .Start(ctx =>
                {
                    var i = 0;
                    foreach (var r in results.SourceObjects.OrderBy(o => o.Name))
                    {
                        if ((i++) % 1000 == 0) ctx.Refresh();

                        table.AddRow(
                            NewMarkup(r.Type, r.Type.ToString()),
                            NewMarkup(r.Type, r.Name),
                            new Markup(r.StatementCount.ToString()),
                            new Markup(r.CoveredStatementCount.ToString()),
                            new Markup(
                                r.CoveragePercent.ToString("0.00") + "%",
                                new Style(r.CoveragePercent > 60 ? Color.Green1 : Color.Default)));
                    }

                    ctx.Refresh();
                });
        }

        static Markup NewMarkup(SourceObjectType type, string text)
            => new Markup(Markup.
                Escape(text),
                new Style(type switch
                {
                    SourceObjectType.TableFunction => Color.Green,
                    SourceObjectType.InlineFunction => Color.Green1,
                    SourceObjectType.ScalarFunction => Color.Green3,
                    SourceObjectType.Procedure => Color.Blue,
                    SourceObjectType.Trigger => Color.Yellow,
                    SourceObjectType.View => Color.Pink1,
                    _ => Color.Default
                }));
    }
}