using SqlServerCoverage.Database;
using SqlServerCoverage.Interfaces;
using System;
using System.Collections.Generic;

namespace SqlServerCoverage
{
    public class CodeCoverage : ICoverageSession, ICoverageSessionController
    {
        private readonly string connectionString;
        private readonly string databaseName;

        public string SessionName { get; private set; }

        public static int SessionTimeout
        {
            get => Connection.TimeOut;
            set => Connection.TimeOut = value;
        }

        private TraceManager Trace => new TraceManager(connectionString, SessionName);

        public static ICoverageSessionController NewController(string connectionString, string databaseName, string traceName = null)
            => new CodeCoverage(connectionString, databaseName, traceName);

        private CodeCoverage(string connectionString, string databaseName, string traceName = null)
        {
            this.connectionString = connectionString;
            this.databaseName = databaseName;
            SessionName = new TraceManager(connectionString, traceName).Name;
        }

        public IReadOnlyList<string> ListSessions() => Trace.ListSessions();

        public ICoverageSession NewSession()
        {
            Trace.RecreateAndStartTrace(databaseName);
            return this;
        }

        public ICoverageSession AttachSession()
        {
            if (!Trace.Exists())
                throw new InvalidOperationException($"Can't attach to session {SessionName} because it does not exist.");

            return this;
        }

        public CoverageResult ReadCoverage(bool waitLatency = true)
        {
            var connection = new Connection(connectionString, databaseName);
            var sourceItems = new SourceReader(connection).GetSourceItems();
            var coveredStatements = Trace.CollectCoverage(waitLatency);

            foreach (var statement in coveredStatements)
            {
                if (sourceItems.TryGetValue(statement.ObjectId, out var b))
                {
                    b.UpdateCoverage(statement);
                }
            }

            return new CoverageResult(sourceItems, databaseName);
        }

        public void StopSession() => Trace.Drop();
    }
}