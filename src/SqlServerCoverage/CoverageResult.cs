using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SqlServerCoverage.Data;

namespace SqlServerCoverage
{
    public class CoverageResult
    {
        private const string SourcePath = "source";
        public int StatementCount { get; }
        public int CoveredStatementCount { get; }
        public double CoveragePercent => StatementCount == 0 ? 0 : ((double)CoveredStatementCount) / StatementCount * 100.0;

        public IReadOnlyList<SourceObject> SourceObjects { get; }

        private readonly string databaseName;

        internal CoverageResult(IEnumerable<SourceObject> sourceObjects, string dbName)
        {
            SourceObjects = sourceObjects.ToArray();
            databaseName = dbName;
            CoveredStatementCount = SourceObjects.Sum(p => p.CoveredStatementCount);
            StatementCount = SourceObjects.Sum(p => p.StatementCount);
        }
        private static readonly string SQLKeywordsRegex =
            "\\b(ALL|AND|ANY|AS|ASC|BETWEEN|CASE|CHECK|DEFAULT|DELETE|DESC|DISTINCT|EXEC|EXISTS|FROM|GROUP BY|HAVING|IN|INTO|INNER JOIN|INSERT|INSERT INTO|IS NULL|IS NOT NULL|JOIN" +
            "LEFT JOIN|LIKE|LIMIT|NOT|OR|ORDER BY|OUTER JOIN|PROCEDURE|RIGHT JOIN|SELECT|DISTINCT|TOP|SET|TABLE|TOP|TRUNCATE|UNION|UNION ALL|UNIQUE|UPDATE|VALUES|VIEW|WHERE" +
            "NULL|BY|ELSE|ELSEIF|FALSE|FOR|GROUP|IF|IS|ON|ORDER|THEN|WHEN|WITH|BEGIN|END|PRINT|RETURN|RETURNS|CREATE|WHERE)\\b";

        public string GetHtml()
        {
            using var writer = new StringWriter();
            WriteHtml(writer);
            return writer.ToString();
        }

        public void WriteHtml(TextWriter writer)
        {
            var coveredObjects = SourceObjects.Count(o => o.IsCovered);
            double objectCount = SourceObjects.Count();
            writer.Write($@"<html>
    <head>
        <title>Code Coverage Results - {databaseName}</title>
        <style>{GetCss()}</style>
    </head>
    <body id=""top"">
        <h1> Coverage Report </h1>
        <p>
            <b>Database: </b> {databaseName}<br>
            <b>Object Coverage: </b> {(coveredObjects*100)/objectCount:0.00}% <br />
            <b>Statement Coverage: </b> {CoveragePercent:0.00}%
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

            foreach (var byType in SourceObjects.GroupBy(o => o.Type))
            {
                double count = byType.Count();
                var coveredCount = byType.Count(o => o.IsCovered);
                var uncoveredCount = byType.Count(o => !o.IsCovered);
                writer.Write($@"
            <tr>
                <td>{byType.Key}</td>
                <td>{coveredCount}</td>
                <td>{uncoveredCount}</td>
                <td>{(coveredCount*100)/count:0.00}</td>
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

            foreach (var sourceObj in SourceObjects.OrderByDescending(p => p.CoveragePercent))
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

            foreach (var sourceObj in SourceObjects)
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

        public string GetOpenCoverXml()
        {
            using var writer = new StringWriter();
            WriteOpenCoverXml(writer);
            return writer.ToString();
        }

        public void WriteOpenCoverXml(TextWriter writer)
        {
            writer.Write($@"
<CoverageSession
    xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <Summary
        numSequencePoints=""{StatementCount}""
        visitedSequencePoints=""{CoveredStatementCount}""
        sequenceCoverage=""{CoveragePercent}""
        numBranchPoints=""0""
        visitedBranchPoints=""0""
        branchCoverage=""0.0""
        maxCyclomaticComplexity=""0""
        minCyclomaticComplexity=""0"" />
    <Modules>
        <Module hash=""ED-DE-ED-DE-ED-DE-ED-DE-ED-DE-ED-DE-ED-DE-ED-DE-ED-DE-ED-DE"">
            <FullName>{databaseName}</FullName>
            <ModuleName>{databaseName}</ModuleName>
            <Files>");

            foreach (var sourceObj in SourceObjects)
            {
                writer.Write($@"
                <File uid=""{sourceObj.ObjectId}"" fullPath=""{SourcePath}/{sourceObj.Name}.sql"" />");
            }

            writer.Write(@"
            </Files>

            <Classes>");

            int i = 1;
            foreach (var sourceObj in SourceObjects)
            {
                var visited = sourceObj.CoveredStatementCount > 0 ? "true" : "false";
                writer.Write(string.Format(@"
                <Class>
                    <FullName>{0}</FullName>
                    <Summary
                        numSequencePoints=""{1}""
                        visitedSequencePoints=""{2}""
                        sequenceCoverage=""{3}""
                        numBranchPoints=""0""
                        visitedBranchPoints=""0""
                        branchCoverage=""0""
                        maxCyclomaticComplexity=""0""
                        minCyclomaticComplexity=""0"" />
                    <Methods>
                        <Method
                            visited=""{4}""
                            sequenceCoverage=""{3}""
                            cyclomaticComplexity=""0""
                            branchCoverage=""0""
                            isConstructor=""false""
                            isStatic=""false""
                            isGetter=""false""
                            isSetter=""false"">
                            <Name>{0}</Name>
                            <FileRef uid=""{5}"" />
                            <Summary
                                numSequencePoints=""{1}""
                                visitedSequencePoints=""{2}""
                                sequenceCoverage=""{3}""
                                numBranchPoints=""0""
                                visitedBranchPoints=""0""
                                branchCoverage=""0""
                                maxCyclomaticComplexity=""0""
                                minCyclomaticComplexity=""0"" />
                            <MetadataToken>01132860</MetadataToken>
                            <SequencePoints>",
                        sourceObj.Name,
                        sourceObj.StatementCount,
                        sourceObj.CoveredStatementCount,
                        sourceObj.CoveragePercent,
                        visited,
                        sourceObj.ObjectId));

                var j = 1;
                foreach (var statement in sourceObj.Statements)
                {
                    var offsets = statement.ToLineAndColumn(sourceObj.Text);

                    writer.Write($@"
                                <SequencePoint
                                    vc=""{statement.HitCount}""
                                    uspid=""{i++}""
                                    ordinal=""{j++}""
                                    offset=""{statement.Offset}""
                                    sl=""{offsets.sl}""
                                    sc=""{offsets.sc}""
                                    el=""{offsets.el}""
                                    ec=""{offsets.ec}"" />");
                }

                writer.Write(@"
                            </SequencePoints>
                        </Method>
                    </Methods>
                </Class>");
            }


            writer.Write(@"
            </Classes>
        </Module>
    </Modules>
</CoverageSession>");
        }

        public string GetSonarGenericXml(string basePath = null)
        {
            using var writer = new StringWriter();
            WriteSonarGenericXml(writer, basePath);
            return writer.ToString();
        }

        public void WriteSonarGenericXml(TextWriter writer, string basePath = null)
        {
            writer.Write($@"
<coverage version=""1"">");

            basePath = string.IsNullOrEmpty(basePath) ? SourcePath : $"{basePath}/{SourcePath}";

            foreach (var sourceObj in SourceObjects)
            {
                writer.Write($@"
    <file path=""{basePath}/{sourceObj.Name}.sql"" >");

                var statements = sourceObj.Statements
                    .GroupBy(s => s.ToLineAndColumn(sourceObj.Text).sl)
                    .Select(g => new {line = g.Key, isCovered = g.Any(s => s.HitCount > 0)});

                foreach (var s in statements.OrderBy(s => s.line))
                {
                    writer.Write($@"
        <lineToCover lineNumber=""{s.line}"" covered=""{s.isCovered}"" />");
                }

                writer.Write(@"
    </file>");
            }
            writer.Write(@"
</coverage>");
        }

        public void WriteSourceFiles(string path)
        {
            path = Path.Combine(path, SourcePath);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            foreach (var obj in SourceObjects)
            {
                File.WriteAllText(Path.Combine(path, $"{obj.Name}.sql"), obj.Text);
            }
        }
    }
}
