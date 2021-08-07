using System.Data;
using Microsoft.Data.SqlClient;

namespace SqlServerCoverage.Database
{
    internal static class DataReaderExtensions
    {
        public static int? TryGetInt(this SqlDataReader reader, string name)
        {
            if (reader.IsDBNull(name)) return null;
            return reader.GetInt32(name);
        }

        public static string TryGetString(this SqlDataReader reader, string name)
        {
            if (reader.IsDBNull(name)) return null;
            return reader.GetString(name);
        }
    }
}
