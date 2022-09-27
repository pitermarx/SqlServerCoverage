using System.IO;

namespace SqlServerCoverage.Result
{
    public static class CoverageResultOpenCoverExtensions
    {
        public static string GetOpenCoverXml(this CoverageResult result)
        {
            using var writer = new StringWriter();
            result.WriteOpenCoverXml(writer);
            return writer.ToString();
        }

        public static void WriteOpenCoverXml(this CoverageResult result, TextWriter writer)
        {
            writer.Write($@"
<CoverageSession
    xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <Summary
        numSequencePoints=""{result.StatementCount}""
        visitedSequencePoints=""{result.CoveredStatementCount}""
        sequenceCoverage=""{result.CoveragePercent}""
        numBranchPoints=""0""
        visitedBranchPoints=""0""
        branchCoverage=""0.0""
        maxCyclomaticComplexity=""0""
        minCyclomaticComplexity=""0"" />
    <Modules>
        <Module hash=""ED-DE-ED-DE-ED-DE-ED-DE-ED-DE-ED-DE-ED-DE-ED-DE-ED-DE-ED-DE"">
            <FullName>{result.DatabaseName}</FullName>
            <ModuleName>{result.DatabaseName}</ModuleName>
            <Files>");

            foreach (var sourceObj in result.SourceObjects)
            {
                writer.Write($@"
                <File uid=""{sourceObj.ObjectId}"" fullPath=""{CoverageResult.SourcePath}/{sourceObj.Name}.sql"" />");
            }

            writer.Write(@"
            </Files>

            <Classes>");

            int i = 1;
            foreach (var sourceObj in result.SourceObjects)
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
    }
}
