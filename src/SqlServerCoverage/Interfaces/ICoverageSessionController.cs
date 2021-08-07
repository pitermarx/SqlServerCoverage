using System.Collections.Generic;

namespace SqlServerCoverage.Interfaces
{
    public interface ICoverageSessionController
    {
        IReadOnlyList<string> ListSessions();

        ICoverageSession NewSession();

        ICoverageSession AttachSession();
    }
}