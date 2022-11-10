using SqlServerCoverage.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlServerCoverage
{
    public class CoverageSessionController
    {
        private readonly string connectionString;

        public static int SessionTimeout
        {
            get => Connection.TimeOut;
            set => Connection.TimeOut = value;
        }

        public CoverageSessionController(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public IReadOnlyList<(string sessionId, string? database)> ListSessions() =>
            new SessionManager(connectionString).ListSessions();

        public CoverageSession NewSession(string databaseName)
        {
            var session = new SessionManager(connectionString);
            var sessionId = session.RecreateAndStartSession(databaseName);
            return new CoverageSession(connectionString, databaseName, sessionId);
        }

        public CoverageSession AttachSession(string sessionId)
        {
            var session = new SessionManager(connectionString, sessionId);
            if (!session.Exists())
                throw new InvalidOperationException(
                    $"Can't attach to session '{sessionId}' because it does not exist."
                );

            var (id, db) = session.ListSessions().First(s => s.sessionId == sessionId);

            if (id is null) throw new Exception($"Session {sessionId} not found");
            if (db is null) throw new Exception($"Database for session {sessionId} not found");

            return new CoverageSession(connectionString, db, id);
        }
    }
}
