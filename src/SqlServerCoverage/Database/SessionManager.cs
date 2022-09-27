using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using SqlServerCoverage.Data;

namespace SqlServerCoverage.Database
{
    internal class SessionManager
    {
        private const string SessionPrefix = "SqlServerCoverageTrace-";
        private const int Latency = 1;
        private readonly string sessionId;
        private readonly Connection connection;

        private string fileName;

        public SessionManager(string connectionString, string sessionName = null)
        {
            sessionId =
                sessionName == null
                    ? $"{SessionPrefix}{Guid.NewGuid():N}"
                    : sessionName.StartsWith(SessionPrefix)
                        ? sessionName
                        : $"{SessionPrefix}{sessionName}";
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
                throw new InvalidOperationException(
                    $"Error while running '{firstLine}'. {ex.Message}",
                    ex
                );
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
                throw new InvalidOperationException(
                    "Unable to use xp_readerrorlog to find log directory to write extended event file"
                );
            }

            return fileName = Path.Combine(logDir, sessionId);
        }

        public List<(string sessionId, string database)> ListSessions()
        {
            return connection.ReadAll(
                @$"
                with traces as (
                    select
                        s.name as tracename,
                        CAST(event_predicate as XML).value('(/*:leaf/*:value)[1]', 'int') as db_id
                    from
                        sys.dm_xe_sessions s
                    join
                        sys.dm_xe_session_events e
                            on e.event_session_address = s.address
                    where
                        s.name like '{SessionPrefix}%'
                )
                select
                    t.tracename,
                    d.name
                from
                    traces t
                left join
                    sys.databases d
                        on d.database_id = t.db_id
                ",
                cmd => (cmd.GetString(0), cmd.GetStringOrNull(1))
            );
        }

        public bool Exists()
        {
            return connection.Execute(
                    $"SELECT name FROM sys.dm_xe_sessions WHERE name = '{sessionId}'",
                    cmd => cmd.ExecuteScalar()
                ) != null;
        }

        /// <sumamry>
        /// Creates an event session to collect trace events
        /// More info about sessions here https://docs.microsoft.com/en-us/sql/relational-databases/extended-events/quick-start-extended-events-in-sql-server
        /// </summary>
        public string RecreateAndStartSession(string databaseName)
        {
            var name = GetFileName();

            if (Exists())
            {
                RunScript($"DROP EVENT SESSION [{this.sessionId}] ON SERVER");
            }

            var dbId = connection
                .Execute($"select db_id('{databaseName}')", cmd => cmd.ExecuteScalar())
                ?.ToString();

            if (string.IsNullOrEmpty(dbId))
            {
                throw new InvalidOperationException(
                    $"Can't start session on database '{databaseName}' because it does not exist."
                );
            }

            RunScript(
                $@"
                CREATE EVENT SESSION [{sessionId}] ON SERVER
                ADD EVENT sqlserver.sp_statement_starting(
                    action (sqlserver.plan_handle, sqlserver.tsql_stack)
                    where ([sqlserver].[database_id]=({dbId}))
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
                )"
            );

            RunScript($"ALTER EVENT SESSION [{sessionId}] ON SERVER state = start");

            return this.sessionId;
        }

        /// <summary> Stops the event session and reads trace events into a list of Covered statements</summary>
        public List<CoverageFragment> CollectCoverage(bool waitLatency)
        {
            // If not wait, the last events may not be collected
            if (waitLatency)
                Thread.Sleep(1000 * Latency);

            var name = GetFileName();
            var records = connection.ReadAll(
                $@"
                    SELECT event_data
                    FROM sys.fn_xe_file_target_read_file(N'{name}*.xel', N'{name}*.xem', null, null);",
                reader =>
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(reader.GetString(0));

                    // not sure why I need to /2 and +2
                    // but it works...
                    return new CoverageFragment(
                        ReadInt(doc, "object_id"),
                        (ReadInt(doc, "offset") / 2),
                        (ReadInt(doc, "offset_end") / 2) + 2
                    );
                }
            );

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
                RunScript($"ALTER EVENT SESSION [{sessionId}] ON SERVER state = stop");
                RunScript($"DROP EVENT SESSION [{sessionId}] ON SERVER");
            }

            var name = GetFileName();

            RunScript("EXEC sp_configure N'show advanced options', 1");
            RunScript("RECONFIGURE; EXEC sp_configure N'xp_cmdshell', 1");
            RunScript(
                $@"
                RECONFIGURE
                DECLARE @cmd VARCHAR(1000)
                SET @cmd = 'del /q ""{name}*.xel""'
                EXEC xp_cmdshell @cmd"
            );
            RunScript(
                @"
                EXEC sp_configure N'xp_cmdshell', 0
                EXEC sp_configure N'show advanced options', 0"
            );
            RunScript("RECONFIGURE");
        }
    }
}
