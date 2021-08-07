using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace SqlServerCoverage.Database
{
    /// Helper methods to execute dbCommands
    internal class Connection
    {
        public static int TimeOut { get; set; } = 30;

        private readonly string connectionString;
        private readonly string databaseName;

        public Connection(string connectionString, string databaseName)
        {
            this.connectionString = connectionString;
            this.databaseName = databaseName;
        }

        public T Execute<T>(string text, Func<SqlCommand, T> action)
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            conn.ChangeDatabase(databaseName);

            using var cmd = conn.CreateCommand();
            cmd.CommandTimeout = TimeOut;
            cmd.CommandText = text;

            return action(cmd);
        }

        public List<T> ReadAll<T>(string text, Func<SqlDataReader, T> action)
        {
            return Execute(text, cmd =>
            {
                using var reader = cmd.ExecuteReader();
                var list = new List<T>();
                while (reader.Read()) list.Add(action(reader));
                return list;
            });
        }
    }
}
