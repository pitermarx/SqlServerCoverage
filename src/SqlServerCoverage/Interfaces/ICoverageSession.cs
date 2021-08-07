using System;

namespace SqlServerCoverage.Interfaces
{
    public interface ICoverageSession
    {
        string SessionName { get; }
        CoverageResult ReadCoverage(bool waitLatency = true);
        void StopSession();
    }
}