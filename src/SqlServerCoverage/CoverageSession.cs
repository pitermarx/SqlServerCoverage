using SqlServerCoverage.Database;
using SqlServerCoverage.Result;
using System.Linq;

namespace SqlServerCoverage
{
    public class CoverageSession
    {
        private readonly string connectionString;

        public string DatabaseName { get; }

        public string SessionId { get; }

        public CoverageSession(string connectionString, string database, string id)
        {
            this.connectionString = connectionString;
            this.DatabaseName = database;
            this.SessionId = id;
        }

        public CoverageResult ReadCoverage(bool waitLatency = true)
        {
            var coveredStatements = new SessionManager(connectionString, SessionId)
                .CollectCoverage(waitLatency);

            var sourceItems = new SourceReader(connectionString, DatabaseName)
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

            return new CoverageResult(sourceItems.Values, DatabaseName);
        }

        public void Stop() => new SessionManager(connectionString, SessionId).Drop();
    }
}
