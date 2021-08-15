using SqlServerCoverage.Database;
using SqlServerCoverage.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

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

        private CodeCoverage(string connectionString, string databaseName, string traceName)
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
            var coveredStatements = Trace.CollectCoverage(waitLatency);
            var sourceItems = new SourceReader(connectionString, databaseName)
                .GetSourceItems()
                .Where(o => o.IsCoverable)
                .ToDictionary(o => o.ObjectId);

            foreach (var statement in coveredStatements)
            {
                if (sourceItems.TryGetValue(statement.ObjectId, out var b))
                {
                    b.UpdateCoverage(statement);
                }
            }

            return new CoverageResult(sourceItems.Values, databaseName);
        }

        public void StopSession() => Trace.Drop();
    }
}