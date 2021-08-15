using Spectre.Console.Cli;
using Spectre.Console;
using SqlServerCoverage.Data;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

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

            [Description("Whether to write a sonar report. Will also write source files.")]
            [CommandOption("--sonar")]
            public bool Sonar { get; init; }

            [Description("The path to prepend to the source files path. Should be absolute or relative to sonar-project.properties.")]
            [CommandOption("--sonar-base")]
            public string SonarBase { get; init; }

            [Description("Whether to write an opencover report. Will also write source files.")]
            [CommandOption("--opencover")]
            public bool OpenCover { get; init; }

            [Description("Whether to write a summary to the console.")]
            [CommandOption("--summary")]
            public bool Summary { get; init; }

            public string HtmlFile => Path.Combine(OutputDir, $"{Database}_Coverage.html");
            public string OpenCoverFile => Path.Combine(OutputDir, $"{Database}_OpenCover.xml");
            public string SonarFile => Path.Combine(OutputDir, $"{Database}_Sonar.xml");

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

                if ((Html || OpenCover || Sonar) && string.IsNullOrEmpty(OutputDir))
                {
                    return ValidationResult.Error("--output is mandatory when exporting to html or opencover xml formats");
                }

                if (!Html && !OpenCover && !Sonar && !Summary)
                {
                    return ValidationResult.Error("Please define --html --opencover --sonar or --summary");
                }

                return ValidationResult.Success();
            }
        }

        private static void EnsureDir(params string[] paths)
        {
            var path = Path.Combine(paths);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
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
                AnsiConsole.MarkupLine("Exporting HTML.");
                EnsureDir(settings.OutputDir);
                using var writer = File.CreateText(settings.HtmlFile);
                    results.WriteHtml(writer);
            }

            if (settings.OpenCover)
            {
                AnsiConsole.MarkupLine("Exporting OpenCover.");
                EnsureDir(settings.OutputDir);
                results.WriteSourceFiles(settings.OutputDir);

                using var writer = File.CreateText(settings.OpenCoverFile);
                    results.WriteOpenCoverXml(writer);
            }

            if (settings.Sonar)
            {
                AnsiConsole.MarkupLine("Exporting Sonar.");
                var sonarBase = settings.SonarBase;

                if (string.IsNullOrEmpty(sonarBase))
                {
                    AnsiConsole.MarkupLine("[yellow]The argument --sonar-base is not filled. Sonar analysis may not find the source files.[/]");
                    AnsiConsole.MarkupLine($"Assuming --sonar-base={settings.OutputDir}");
                    sonarBase = settings.OutputDir;
                }

                EnsureDir(settings.OutputDir);
                results.WriteSourceFiles(settings.OutputDir);

                using var writer = File.CreateText(settings.SonarFile);
                    results.WriteSonarGenericXml(writer, sonarBase);
            }

            return 0;
        }

        private static void RenderSummary(CoverageResult results)
        {
            AnsiConsole.Render(new Rule("Coverage Summary").Alignment(Justify.Left));

            AnsiConsole.Render(new BarChart
                {
                    Label = $"{results.CoveragePercent:0.00}% Coverage"
                }
                .LeftAlignLabel()
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