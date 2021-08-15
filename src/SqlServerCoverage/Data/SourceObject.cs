using System;
using System.Collections.Generic;

namespace SqlServerCoverage.Data
{
    public class SourceObject
    {
        public static readonly SourceObject Empty = new SourceObject(Array.Empty<Statement>(), string.Empty, string.Empty, -1, "UNKNOWN");

        public string Text { get; }
        public string Name { get; }
        public SourceObjectType Type { get; }
        public int ObjectId { get; }
        public bool IsCovered { get; set; }
        public int CoveredStatementCount { get; private set; }
        public int StatementCount => Statements.Count;
        public double CoveragePercent => ((double)CoveredStatementCount) / StatementCount * 100.0;

        public IReadOnlyList<Statement> Statements { get; }

        // Views, Inline functions and scalar functions are not detected by the (ADD EVENT sqlserver.sp_statement_starting)
        public bool IsCoverable =>
            Type == SourceObjectType.TableFunction ||
            Type == SourceObjectType.Procedure ||
            Type == SourceObjectType.Trigger;

        internal SourceObject(
            IReadOnlyList<Statement> statements,
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

        internal void UpdateCoverage(CoverageFragment fragment)
        {
            foreach (var statement in Statements)
            {
                if (fragment.Covers(statement))
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