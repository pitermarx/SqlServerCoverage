using Spectre.Cli;

namespace SqlServerCoverage.CommandLine
{
    public class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandApp();

            app.Configure(config =>
            {
                config.AddCommand<StartCoverageCommand>("start").WithDescription("Starts a coverage session.");
                config.AddCommand<CollectCoverageCommand>("collect").WithDescription("Collects coverage data and outputs to various formats.");
                config.AddCommand<StopCoverageCommand>("stop").WithDescription("Stops the coverage session.");
                config.AddCommand<ListSessionsCommand>("list").WithDescription("Lists the open sessions.");
            });

            return app.Run(args);
        }
    }
}