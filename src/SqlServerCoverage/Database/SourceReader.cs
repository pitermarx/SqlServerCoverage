using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SqlServerCoverage.Data;
using SqlServerCoverage.Parsers;

namespace SqlServerCoverage.Database
{
    internal class SourceReader
    {
        private readonly Connection database;

        public SourceReader(Connection database) => this.database = database;

        ///<summary>Enumerates all db objects and creates a dictionary in which the key is the objectId</summary>
        public IReadOnlyDictionary<int, SourceObject> GetSourceItems()
        {
            var compatibility = database.Execute(
                "select compatibility_level from sys.databases where database_id = db_id();",
                cmd => cmd.ExecuteScalar()?.ToString());
            var version = int.TryParse(compatibility, out int v) ? v : 130;
            var parser = new StatementParser(version);

            var sourceItems = database
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
                    //WHERE sm.object_id NOT IN (SELECT object_id FROM sys.objects WHERE type = 'IF')", // Why are inline table functions excluded?
                    reader =>
                    {
                        if (reader.TryGetString("object_name") is string name &&
                            reader.TryGetInt("object_id") is int id)
                        {
                            var quoted = reader.GetBoolean("uses_quoted_identifier");
                            var text = reader.TryGetString("definition") ?? string.Empty;
                            var type = reader.GetString("type");

                            if (!text.EndsWith("\r\n\r\n"))
                                text += "\r\n\r\n";

                            var statements = parser.ParseStatements(text, quoted);
                            return new SourceObject(statements, text, name, id, type);
                        }

                        return null;
                    });

            return sourceItems
                .Where(p => p?.StatementCount > 0)
                .ToDictionary(b => b.ObjectId);
        }
   }
}