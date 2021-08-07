using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SqlServerCoverage.Data;

namespace SqlServerCoverage
{
    public class CoverageResult
    {
        public int StatementCount { get; }
        public int CoveredStatementCount { get; }
        public double CoveragePercent => StatementCount == 0 ? 0 : ((double)CoveredStatementCount) / StatementCount * 100.0;

        public IEnumerable<SourceObject> SourceObjects => objects.Values;

        private readonly IReadOnlyDictionary<int, SourceObject> objects;

        private readonly string databaseName;

        internal CoverageResult(IReadOnlyDictionary<int, SourceObject> sourceObjects, string dbName)
        {
            objects = sourceObjects;
            databaseName = dbName;
            CoveredStatementCount = objects.Values.Sum(p => p.CoveredStatementCount);
            StatementCount = objects.Values.Sum(p => p.StatementCount);
        }

        public string Html()
        {
            using var writer = new StringWriter();
            Html(writer);
            return writer.ToString();
        }

        public void Html(TextWriter writer)
        {
            writer.Write(@"
<html>
    <head>
        <title>SqlServerCoverage Code Coverage Results</title>
        <style>
            :root{
                --text: #333333;
                --text-med: #888888;
                --text-light: #cccccc;
                --text-lighter: #eeeeee;
                --blue: #3498db;
                --dark-blue: #2980b9;
                --yellow: #ffeaa7;
                --red: #e45748;
                --green: #38cc51;
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
            .right{
                float: right;
            }
        </style>
    </head>
    <body id=""top"">");

            writer.Write($@"
        <table>
            <thead>
                <td>Object Name</td>
                <td>Statement Count</td>
                <td>Covered Statement Count</td>
                <td>Coverage %</td>
            </thead>
            <tr>
                <td><b>Total</b></td>
                <td>{StatementCount}</td>
                <td>{CoveredStatementCount}</td>
                <td>{CoveragePercent:0.00}</td>
            </tr>");

            foreach (var sourceObj in objects.Values.OrderByDescending(p => p.CoveragePercent))
            {
                writer.Write($@"
            <tr>
                <td><a href=""#{sourceObj.Name}"">{sourceObj.Name}</a></td>
                <td>{sourceObj.StatementCount}</td>
                <td>{sourceObj.CoveredStatementCount}</td>
                <td>{sourceObj.CoveragePercent:0.00}</td>
            </tr>");
            }

            writer.Write(@"
        </table>");

            foreach (var sourceObj in objects.Values)
            {
                writer.Write(string.Format(@"
        <hr />
        <details>
            <summary><a name=""{0}"">{0}</a> <a href=""#top"" class=right>Scroll Top</a></summary>
            <pre>", sourceObj.Name));

                if (!sourceObj.IsCovered)
                {
                    writer.Write(sourceObj.Text);
                }
                else
                {
                    var builder = new StringBuilder(sourceObj.Text);

                    foreach (var statement in sourceObj.Statements.OrderByDescending(p => p.Offset))
                    {
                        var color = statement.HitCount > 0 ? "green" : "red";
                        builder.Remove(statement.Offset, statement.Length);
                        builder.Insert(statement.Offset, $@"<mark class={color}>{statement.Text}</mark>");
                    }

                    writer.Write(builder.ToString());
                }

                writer.Write(@"
                </pre>
            </details>");
            }

            writer.Write(@"
    </body>
</html>");
        }

        public string OpenCoverXml()
        {
            using var writer = new StringWriter();
            OpenCoverXml(writer);
            return writer.ToString();
        }

        public void OpenCoverXml(TextWriter writer)
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

            foreach (var sourceObj in objects)
            {
                writer.Write($@"
                <File uid=""{sourceObj.Key}"" fullPath=""source/{sourceObj.Value.Name}.sql"" />");
            }

            writer.Write(@"
            </Files>

            <Classes>");

            int i = 1;
            foreach (var item in objects)
            {
                var sourceObj = item.Value;
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
                            isGetter=""true""
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
                            <MetadataToken>01041980</MetadataToken>
                            <SequencePoints>",
                        sourceObj.Name,
                        sourceObj.StatementCount,
                        sourceObj.CoveredStatementCount,
                        sourceObj.CoveragePercent,
                        visited,
                        item.Key));

                var j = 1;
                foreach (var statement in sourceObj.Statements)
                {
                    var offsets = new OpenCoverOffsets(statement.Offset, statement.Length, sourceObj.Text);
                    writer.Write($@"
                                <SequencePoint
                                    vc=""{statement.HitCount}""
                                    uspid=""{i++}""
                                    ordinal=""{j++}""
                                    offset=""{statement.Offset}""
                                    sl=""{offsets.StartLine}""
                                    sc=""{offsets.StartColumn}""
                                    el=""{offsets.EndLine}""
                                    ec=""{offsets.EndColumn}"" />");
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
    }
}
