using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using SqlServerCoverage.Data;

namespace SqlServerCoverage.Database
{
    internal class TraceManager
    {
        private const string TracePrefix = "SqlServerCoverageTrace-";
        private const int Latency = 1;

        public string Name { get; }

        private readonly Connection connection;

        private string fileName;

        public TraceManager(string connectionString, string traceName = null)
        {
            Name =
                traceName == null ? $"{TracePrefix}{Guid.NewGuid():N}" :
                traceName.StartsWith(TracePrefix) ? traceName :
                $"{TracePrefix}{traceName}";
            connection = new Connection(connectionString, "master");
        }

        private void RunScript(string query)
        {
            try
            {
                connection.Execute(query, cmd => cmd.ExecuteNonQuery());
            }
            catch (Exception ex)
            {
                var firstLine = query.Split("\n").First();
                throw new InvalidOperationException($"Error while running '{firstLine}'. {ex.Message}", ex);
            }
        }

        private string GetFileName()
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                return fileName;
            }

            string getLogDir = @"EXEC xp_readerrorlog 0, 1, N'Logging SQL Server messages in file'";
            var logDir = connection
                .ReadAll(getLogDir, reader => reader.GetString(2))
                .FirstOrDefault()
                ?.ToUpper()
                .Replace("LOGGING SQL SERVER MESSAGES IN FILE".ToUpper(), "")
                .Replace("'", "")
                .Replace("ERRORLOG.", "")
                .Replace("ERROR.LOG", "")
                .Trim();

            if (string.IsNullOrEmpty(logDir))
            {
                throw new InvalidOperationException("Unable to use xp_readerrorlog to find log directory to write extended event file");
            }

            return fileName = Path.Combine(logDir, Name);
        }

        public List<string> ListSessions()
        {
            return connection.ReadAll(
                $"SELECT name FROM sys.dm_xe_sessions WHERE name like '{TracePrefix}%'",
                cmd => cmd.GetString(0));
        }

        public bool Exists()
        {
            return connection.Execute(
                $"SELECT name FROM sys.dm_xe_sessions WHERE name = '{Name}'",
                cmd => cmd.ExecuteScalar()) != null;
        }

        /// <sumamry>
        /// Creates an event session to collect trace events
        /// More info about sessions here https://docs.microsoft.com/en-us/sql/relational-databases/extended-events/quick-start-extended-events-in-sql-server
        /// </summary>
        public void RecreateAndStartTrace(string databaseName)
        {
            var name = GetFileName();

            if (Exists())
            {
                RunScript($"DROP EVENT SESSION [{Name}] ON SERVER");
            }

            var id = connection
                .Execute(
                    $"select db_id('{databaseName}')",
                    cmd => cmd.ExecuteScalar())
                ?.ToString();

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("Can't start session on database SqlServerCoverageTest because it does not exist.");
            }

            RunScript(
                $@"
                CREATE EVENT SESSION [{Name}] ON SERVER
                ADD EVENT sqlserver.sp_statement_starting(
                    action (sqlserver.plan_handle, sqlserver.tsql_stack)
                    where ([sqlserver].[database_id]=({id}))
                )
                ADD TARGET package0.asynchronous_file_target(SET filename='{name}.xel')
                WITH (
                    MAX_MEMORY=100 MB,
                    EVENT_RETENTION_MODE=NO_EVENT_LOSS,
                    MAX_DISPATCH_LATENCY={Latency} SECONDS,
                    MAX_EVENT_SIZE=0 KB,
                    MEMORY_PARTITION_MODE=NONE,
                    TRACK_CAUSALITY=OFF,
                    STARTUP_STATE=OFF
                )");

            RunScript($"ALTER EVENT SESSION [{Name}] ON SERVER state = start");
        }

        /// <summary> Stops the event session and reads trace events into a list of Covered statements</summary>
        public List<CoverageFragment> CollectCoverage(bool waitLatency)
        {
            // If not wait, the last events may not be collected
            if (waitLatency) Thread.Sleep(1000 * Latency);

            var name = GetFileName();
            var records = connection
                .ReadAll($@"
                    SELECT event_data
                    FROM sys.fn_xe_file_target_read_file(N'{name}*.xel', N'{name}*.xem', null, null);",
                    reader =>
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(reader.GetString(0));

                        return new CoverageFragment(
                            ReadInt(doc, "object_id"),
                            ReadInt(doc, "offset"),
                            ReadInt(doc, "offset_end"));
                    });

            return records;

            static int ReadInt(XmlDocument doc, string name)
            {
                var node = doc.SelectNodes($"/event/data[@name='{name}']").Item(0);
                return int.Parse(node.InnerText);
            }
        }

        /// <summary>Drops the event session</summary>
        public void Drop()
        {
            if (Exists())
            {
                RunScript($"ALTER EVENT SESSION [{Name}] ON SERVER state = stop");
                RunScript($"DROP EVENT SESSION [{Name}] ON SERVER");
            }

            try
            {
                var name = GetFileName();

                // In the event that SQL server is on the same machine as this
                // try to delete the files
                // but ignore failures

                var info = new FileInfo(name);
                var dir = new DirectoryInfo(info.DirectoryName);
                var wildcard = info.Name + "*.*";
                foreach (var file in dir.EnumerateFiles(wildcard))
                    File.Delete(file.FullName);
            }
            catch
            {
                // Swallow
            }
        }
    }
}