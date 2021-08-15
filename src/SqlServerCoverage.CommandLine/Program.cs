using Spectre.Console.Cli;

namespace SqlServerCoverage.CommandLine
{
    public class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandApp();

            app.Configure(config =>
            {
                config.AddCommand<StartCoverageCommand>("start").WithDescription("Starts a coverage session. Outputs the session id.");
                config.AddCommand<CollectCoverageCommand>("collect").WithDescription("Collects coverage data and outputs to various formats.");
                config.AddCommand<StopCoverageCommand>("stop").WithDescription("Stops the coverage session with a given id.");
                config.AddCommand<StopAllCoverageCommand>("stop-all").WithDescription("Stops the all the coverage sessions.");
                config.AddCommand<ListSessionsCommand>("list").WithDescription("Lists the open sessions.");

                config.SetApplicationName("dotnet sql-coverage");
                config.AddExample(new [] { "start", "--connection-string=\"Data Source=(local);Integrated Security=True\"", "--database=DatabaseName" });
                config.AddExample(new [] { "collect", "--connection-string=...", "--database=DatabaseName", "--id={ID from start command} --html" });
                config.AddExample(new [] { "stop", "--connection-string=...", "--id={ID from start command}" });
                config.AddExample(new [] { "stop-all", "--connection-string=..." });
                config.AddExample(new [] { "list", "--connection-string=..." });
            });

            return app.Run(args);
        }
    }
}