using System.Collections.Generic;
using System.IO;
using System.Linq;
using SqlServerCoverage.Data;

namespace SqlServerCoverage.Result
{
    public class CoverageResult
    {
        public const string SourcePath = "source";
        public int StatementCount { get; }
        public int CoveredStatementCount { get; }
        public double CoveragePercent => StatementCount == 0 ? 0 : ((double)CoveredStatementCount) / StatementCount * 100.0;

        public IReadOnlyList<SourceObject> SourceObjects { get; }

        public string DatabaseName { get; }

        internal CoverageResult(IEnumerable<SourceObject> sourceObjects, string dbName)
        {
            SourceObjects = sourceObjects.ToArray();
            DatabaseName = dbName;
            CoveredStatementCount = SourceObjects.Sum(p => p.CoveredStatementCount);
            StatementCount = SourceObjects.Sum(p => p.StatementCount);
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
