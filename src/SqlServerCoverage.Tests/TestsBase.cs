using Microsoft.Data.SqlClient;
using System;
using System.IO;
using VerifyTests;

namespace SqlServerCoverage.Tests
{
    public class TestsBase
    {
        protected const string SprocText =
            @"CREATE PROC TestProcedureForCoverage
    (@value int)
AS
BEGIN
    IF (@value = 1)
        BEGIN
            SELECT 10
        END
    ELSE
        BEGIN
            SELECT 20
        END
END";
        public static string ConnectionString
            => (Environment.GetEnvironmentVariable("ConnectionStringForTests")
                ?? "Data Source=(local);Integrated Security=True") + ";TrustServerCertificate=True";
        public const string DatabaseName = "SqlServerCoverageTests";

        protected TestsBase() { }

        public static VerifySettings VerifySettings()
        {
            var settings = new VerifySettings();
            settings.UseDirectory("Snapshots");
            return settings;
        }

        public static CoverageSessionController NewCoverageController() =>
            new CoverageSessionController(ConnectionString);

        public static void WithTestData(Action<CoverageSession> action)
        {
            WithDatabase(() =>
            {
                var session = NewCoverageController().NewSession(DatabaseName);

                using var stream = typeof(TestsBase).Assembly.GetManifestResourceStream(
                    "SqlServerCoverage.Tests.test_data.sql"
                );
                using var reader = new StreamReader(stream);

                foreach (var statement in reader.ReadToEnd().Split("GO"))
                    Execute(statement.Trim(), cmd => cmd.ExecuteNonQuery());

                try
                {
                    action(session);
                }
                finally
                {
                    session.Stop();
                }
            });
        }

        public static void WithTraceAndSproc(Action<CoverageSession> action)
        {
            WithDatabase(() =>
            {
                Execute(SprocText, cmd => cmd.ExecuteNonQuery());

                var session = NewCoverageController().NewSession(DatabaseName);

                try
                {
                    action(session);
                }
                finally
                {
                    session.Stop();
                }
            });
        }

        public static void WithNewSession(Action<CoverageSession> action)
        {
            WithDatabase(() =>
            {
                var session = NewCoverageController().NewSession(DatabaseName);

                try
                {
                    action(session);
                }
                finally
                {
                    session.Stop();
                }
            });
        }

        public static void WithDatabase(Action action)
        {
            var dropSql =
                $@"if (select DB_ID('{DatabaseName}')) is not null
                begin
                    alter database [{DatabaseName}] set offline with rollback immediate;
                    alter database [{DatabaseName}] set online;
                    drop database [{DatabaseName}];
                end";

            Execute(dropSql, c => c.ExecuteNonQuery(), false);
            Execute($"CREATE DATABASE {DatabaseName}", c => c.ExecuteNonQuery(), false);

            try
            {
                action();
            }
            finally
            {
                Execute(dropSql, c => c.ExecuteNonQuery(), false);
            }
        }

        public static T Execute<T>(string text, Func<SqlCommand, T> action, bool changeDb = true)
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            if (changeDb)
                conn.ChangeDatabase(DatabaseName);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = text;

            return action(cmd);
        }
    }
}
