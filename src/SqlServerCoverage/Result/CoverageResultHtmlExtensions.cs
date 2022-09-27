using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SqlServerCoverage.Result
{
    public static class CoverageResultHtmlExtensions
    {
        private static readonly string SQLKeywordsRegex =
            "\\b(ALL|AND|ANY|AS|ASC|BETWEEN|CASE|CHECK|DEFAULT|DELETE|DESC|DISTINCT|EXEC|EXISTS|FROM|GROUP BY|HAVING|IN|INTO|INNER JOIN|INSERT|INSERT INTO|IS NULL|IS NOT NULL|JOIN" +
            "LEFT JOIN|LIKE|LIMIT|NOT|OR|ORDER BY|OUTER JOIN|PROCEDURE|RIGHT JOIN|SELECT|DISTINCT|TOP|SET|TABLE|TOP|TRUNCATE|UNION|UNION ALL|UNIQUE|UPDATE|VALUES|VIEW|WHERE" +
            "NULL|BY|ELSE|ELSEIF|FALSE|FOR|GROUP|IF|IS|ON|ORDER|THEN|WHEN|WITH|BEGIN|END|PRINT|RETURN|RETURNS|CREATE|WHERE)\\b";

        public static string GetHtml(this CoverageResult result)
        {
            using var writer = new StringWriter();
            result.WriteHtml(writer);
            return writer.ToString();
        }

        public static void WriteHtml(this CoverageResult result, TextWriter writer)
        {
            var coveredObjects = result.SourceObjects.Count(o => o.IsCovered);
            double objectCount = result.SourceObjects.Count();
            writer.Write($@"<html>
    <head>
        <title>Code Coverage Results - {result.DatabaseName}</title>
        <style>{GetCss()}</style>
    </head>
    <body id=""top"">
        <h1> Coverage Report </h1>
        <p>
            <b>Database: </b> {result.DatabaseName}<br>
            <b>Object Coverage: </b> {(coveredObjects * 100) / objectCount:0.00}% <br />
            <b>Statement Coverage: </b> {result.CoveragePercent:0.00}%
        </p>
        <p> <mark>Attention!</mark> <br>
            This tool only collects coverage for Stored Procedures, Triggers and table valued functions. <br>
            View and Inline/Scalar function executions are not tracked
        </p>
        <h2> Object breakdown </h2>
        <table>
            <thead>
                <td>Type</td>
                <td>Covered objects</td>
                <td>Uncovered objects</td>
                <td>Coverage %</td>
            </thead>");

            foreach (var byType in result.SourceObjects.GroupBy(o => o.Type))
            {
                double count = byType.Count();
                var coveredCount = byType.Count(o => o.IsCovered);
                var uncoveredCount = byType.Count(o => !o.IsCovered);
                writer.Write($@"
            <tr>
                <td>{byType.Key}</td>
                <td>{coveredCount}</td>
                <td>{uncoveredCount}</td>
                <td>{(coveredCount * 100) / count:0.00}</td>
            </tr>");
            }

            writer.Write($@"
        </table>
        <h2> Statement breakdown</h2>
        <table>
            <thead>
                <td>Type</td>
                <td>Object Name</td>
                <td># Statements</td>
                <td># Covered Statements</td>
                <td>Coverage %</td>
            </thead>");

            foreach (var sourceObj in result.SourceObjects.OrderByDescending(p => p.CoveragePercent))
            {
                writer.Write($@"
            <tr>
                <td>{sourceObj.Type}</td>
                <td><a href=""#{sourceObj.Name}"">{sourceObj.Name}</a></td>
                <td>{sourceObj.StatementCount}</td>
                <td>{sourceObj.CoveredStatementCount}</td>
                <td>{sourceObj.CoveragePercent:0.00}</td>
            </tr>");
            }

            writer.Write(@"
        </table>");

            foreach (var sourceObj in result.SourceObjects)
            {
                writer.Write(string.Format(@"
        <hr />
        <details>
            <summary><a name=""{0}"">{0}</a> <a href=""#top"" class=right>Scroll Top</a></summary>
            <pre>", sourceObj.Name));

                var builder = new StringBuilder(sourceObj.Text);

                foreach (var statement in sourceObj.Statements.OrderByDescending(p => p.Offset))
                {
                    var color = statement.HitCount > 0 ? "green" : "red";
                    builder.Remove(statement.Offset, statement.Text.Length);
                    builder.Insert(statement.Offset, $@"<mark class={color}>{statement.Text}</mark>");
                }

                var highlightedKeywords = Regex.Replace(builder.ToString(), SQLKeywordsRegex, @"<b>$1</b>");
                writer.Write(highlightedKeywords);
                writer.Write(@"</pre>
            </details>");
            }

            writer.Write(@"
    </body>
</html>");
            static string GetCss()
            {
                return @"
            /*! stylize.css v1.0.0 | License MIT | https://github.com/vasanthv/stylize.css */
            :root{
                --text: #333333;
                --text-med: #888888;
                --text-light: #cccccc;
                --text-lighter: #eeeeee;
                --blue: #3498db;
                --dark-blue: #2980b9;
                --yellow: #ffeaa7;
                --red: #ff8577;
                --green: #00ea28;
                --border-radius: 3px;
            }
            body{
                font-family: ""Segoe UI"",Roboto,Oxygen-Sans,Ubuntu,Cantarell,""Helvetica Neue"",sans-serif;
                line-height: 1.5;
                font-size: 1em;
                color: var(--text);
                margin: 10px 50px;
                -webkit-text-size-adjust: 100 %;
            }
            h1{
                font-size: 2em; /* h1 inside section is treated different in some browsers */
                margin: 0.67em 0;
            }
            h2{
                font-size: 1.5em;
                margin: 0.83em 0;
            }
            h3{
                font-size: 1.17em;
                margin: 1em 0;
            }
            a{
                color:var(--blue);
                background-color: transparent;
            }
            a:visited{
                color:var(--dark-blue);
            }
            hr{
                border: 1px solid var(--text-med);
                border-bottom: 0px;
                height: 0px;
            }
            mark{
                background-color: var(--yellow);
                color:#333333;
            }
            mark.red{ background-color: var(--red); }
            mark.green{ background-color: var(--green); }
            mark.blue{ background-color: var(--blue); }
            pre{
                font-family: monospace;
                border: 1px solid var(--text-light);
                background-color: var(--text-lighter);
                padding: 0.5em;
                border-radius: var(--border-radius);
                font-size: 1em;
                white-space: pre-wrap;
                word-wrap: break-word;
            }

            table{
                border-collapse: collapse;
            }
            table,
            table th,
            table td{
                border-bottom: 1px solid var(--text-light);
                padding: 0.33em 0.66em;
                text-align: left;
                vertical-align: middle;
            }

            /* Custom */
            pre b { color: blue }
            .right{
                float: right;
            }";
            }
        }
    }
}
