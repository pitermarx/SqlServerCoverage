using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlServerCoverage.Data;

namespace SqlServerCoverage.Parsers
{
    internal class StatementVisitor : TSqlFragmentVisitor
    {
        private readonly string script;
        public readonly List<Statement> Statements = new List<Statement>();

        public StatementVisitor(string script) => this.script = script;

        public override void Visit(TSqlStatement node)
        {
            if (!ShouldEnumerateChildren(node))
            {
                return;
            }

            base.Visit(node);

            if (IsIgnoredType(node))
            {
                return;
            }

            var offset = node.StartOffset;
            var length = GetLength(node);
            var statementText = script.Substring(offset, length);
            Statements.Add(new Statement(statementText, offset));
        }

        private bool ShouldEnumerateChildren(TSqlStatement statement) => statement switch
        {
            CreateViewStatement _ => false,
            _ => true
        };

        private bool IsIgnoredType(TSqlStatement statement) => statement switch
        {
            DeclareTableVariableStatement _ => true,
            CreateProcedureStatement _      => true,
            CreateFunctionStatement _       => true,
            CreateTriggerStatement _        => true,
            BeginEndBlockStatement _        => true,
            TryCatchStatement _             => true,
            LabelStatement _                => true,
            _                               => false
        };

        private int GetLength(TSqlStatement statement) => statement switch
        {
            IfStatement stmt    => stmt.Predicate.StartOffset + stmt.Predicate.FragmentLength - statement.StartOffset,
            WhileStatement stmt => stmt.Predicate.StartOffset + stmt.Predicate.FragmentLength - statement.StartOffset,
            _                   => statement.FragmentLength
        };
    }
}