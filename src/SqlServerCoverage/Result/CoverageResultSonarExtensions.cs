using System.Collections.Generic;
using System.IO;
using System.Linq;
using SqlServerCoverage.Data;

namespace SqlServerCoverage.Result
{
    public static class CoverageResultSonarExtensions
    {
        public static string GetSonarGenericXml(this CoverageResult result, string? basePath = null)
        {
            using var writer = new StringWriter();
            result.WriteSonarGenericXml(writer, basePath);
            return writer.ToString();
        }

        public static void WriteSonarGenericXml(this CoverageResult result, TextWriter writer, string? basePath = null)
        {
            writer.Write($@"
<coverage version=""1"">");

            basePath = string.IsNullOrEmpty(basePath) ? CoverageResult.SourcePath : $"{basePath}/{CoverageResult.SourcePath}";

            foreach (var sourceObj in result.SourceObjects)
            {
                writer.Write($@"
    <file path=""{basePath}/{sourceObj.Name}.sql"" >");

                var statements = sourceObj.Statements
                    .GroupBy(s => s.ToLineAndColumn(sourceObj.Text).sl)
                    .Select(g => new { line = g.Key, isCovered = g.Any(s => s.HitCount > 0) });

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
    }
}
