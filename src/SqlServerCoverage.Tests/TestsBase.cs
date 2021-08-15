using SqlServerCoverage.Interfaces;
using System;
using System.Data.SqlClient;
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
        public const string ConnectionString = "Data Source=(local);Integrated Security=True";
        public const string DatabaseName = "SqlServerCoverageTests";

        protected TestsBase() { }

        public static VerifySettings VerifySettings()
        {
            var settings = new VerifySettings();
            settings.UseDirectory("Snapshots");
            return settings;
        }

        public static ICoverageSessionController NewCoverageController() => CodeCoverage.NewController(ConnectionString, DatabaseName);

        public static void WithTestData(Action<ICoverageSession> action)
        {
            WithDatabase(() =>
            {
                var session = NewCoverageController().NewSession();

                using var stream = typeof(TestsBase).Assembly.GetManifestResourceStream("SqlServerCoverage.Tests.test_data.sql");
                using var reader = new StreamReader(stream);

                foreach (var statement in reader.ReadToEnd().Split("GO"))
                    Execute(statement.Trim(), cmd => cmd.ExecuteNonQuery());

                try
                {
                    action(session);
                }
                finally
                {
                    session.StopSession();
                }
            });
        }

        public static void WithTraceAndSproc(Action<ICoverageSession> action)
        {
            WithDatabase(() =>
            {
                Execute(SprocText, cmd => cmd.ExecuteNonQuery());

                var session = NewCoverageController().NewSession();

                try
                {
                    action(session);
                }
                finally
                {
                    session.StopSession();
                }
            });
        }

        public static void WithNewSession(Action<ICoverageSession> action)
        {
            WithDatabase(() =>
            {
                var session = NewCoverageController().NewSession();

                try
                {
                    action(session);
                }
                finally
                {
                    session.StopSession();
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

            if (changeDb) conn.ChangeDatabase(DatabaseName);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = text;

            return action(cmd);
        }
    }
}