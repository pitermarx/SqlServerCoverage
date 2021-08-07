using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlServerCoverage.Data;
using System;

namespace SqlServerCoverage.Parsers
{
    internal class StatementParser
    {
        private readonly int version;

        public StatementParser(int version) => this.version = version;

        public List<Statement> ParseStatements(string script, bool quotedIdentifier)
        {
            var visitor = new StatementVisitor(script);

            MakeSqlParser(quotedIdentifier)
                .Parse(new StringReader(script), out _)
                ?.Accept(visitor);

            return visitor.Statements;
        }

        private TSqlParser MakeSqlParser(bool quoted) => version switch
        {
            90 => new TSql90Parser(quoted),
            100 => new TSql100Parser(quoted),
            110 => new TSql110Parser(quoted),
            120 => new TSql120Parser(quoted),
            130 => new TSql130Parser(quoted),
            140 => new TSql140Parser(quoted),
            150 => new TSql150Parser(quoted),
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null),
        };
    }
}