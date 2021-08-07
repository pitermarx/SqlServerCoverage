using System.Collections.Generic;

namespace SqlServerCoverage.Data
{
    public class SourceObject
    {
        public string Text { get; }
        public string Name { get; }
        public SourceObjectType Type { get; }
        public int ObjectId { get; }
        public bool IsCovered { get; set; }
        public int CoveredStatementCount { get; private set; }
        public int StatementCount => Statements.Count;
        public double CoveragePercent => ((double)CoveredStatementCount) / StatementCount * 100.0;

        public IReadOnlyList<Statement> Statements { get; }

        internal SourceObject(
            List<Statement> statements,
            string text,
            string objectName,
            int objectId,
            string type)
        {
            Text = text.TrimEnd();
            Type = ParseObjectType(type);
            Name = objectName;
            ObjectId = objectId;
            Statements = statements;
        }

        private static SourceObjectType ParseObjectType(string type) => type.Trim() switch
        {
            "IF" => SourceObjectType.InlineFunction,
            "FN" => SourceObjectType.ScalarFunction,
            "P" => SourceObjectType.Procedure,
            "TF" => SourceObjectType.TableFunction,
            "V" => SourceObjectType.View,
            "TR" => SourceObjectType.Trigger,
            _ => SourceObjectType.Unknown
        };

        internal void UpdateCoverage(CoverageFragment coverage)
        {
            foreach (var statement in Statements)
            {
                if (coverage.Includes(statement))
                {
                    IsCovered = true;
                    if (statement.HitCount == 0)
                    {
                        CoveredStatementCount += 1;
                    }
                    statement.HitCount += 1;
                    return;
                }
            }
        }
   }
}