using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlServerCoverage.Data;
using SqlServerCoverage.TSql;

namespace SqlServerCoverage.Database
{
    internal class SourceReader
    {
        private readonly Connection connection;

        public SourceReader(string connectionString, string databaseName)
            => connection = new Connection(connectionString, databaseName);

        ///<summary>Enumerates all db objects and creates a dictionary in which the key is the objectId</summary>
        public List<SourceObject> GetSourceItems()
        {
            var compatibility = connection.Execute(
                "select compatibility_level from sys.databases where database_id = db_id();",
                cmd => cmd.ExecuteScalar()?.ToString());
            var version = int.TryParse(compatibility, out int v) ? v : 130;
            var quotedParser = CreateParser(version, true);
            var unquotedParser = CreateParser(version, false);

            return connection
                .ReadAll(@"
                    SELECT
                        sm.object_id,
                        ISNULL('[' + OBJECT_SCHEMA_NAME(sm.object_id) + '].[' + OBJECT_NAME(sm.object_id) + ']', '[' + st.name + ']') object_name,
                        sm.definition,
                        sm.uses_quoted_identifier,
                        o.type
                    FROM sys.sql_modules sm
                    JOIN sys.objects o on sm.object_id = o.object_id
                    LEFT JOIN sys.triggers st
                        ON st.object_id = sm.object_id",
                    reader =>
                    {
                        if (reader.GetIntOrNull("object_id") is int id &&
                            reader.GetStringOrNull("object_name") is string name &&
                            reader.GetStringOrNull("definition") is string text)
                        {
                            var quoted = reader.GetBoolOrNull("uses_quoted_identifier") ?? false;
                            var type = reader.GetStringOrNull("type") ?? "UNKNOWN";
                            var statements = Parse(text, quoted);
                            return new SourceObject(statements, text, name, id, type);
                        }

                        return SourceObject.Empty;
                    });

            IReadOnlyList<Statement> Parse(string text, bool? quoted)
            {
                var parser = quoted == true ? quotedParser : unquotedParser;
                using var stringReader = new StringReader(text);
                var fragment = parser.Parse(stringReader, out _);
                var visitor = new StatementVisitor(text);
                fragment?.Accept(visitor);
                return visitor.Statements;
            }
        }

        private static TSqlParser CreateParser(int version, bool quoted) => version switch
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