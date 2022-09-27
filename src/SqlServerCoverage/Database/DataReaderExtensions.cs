using System.Data;
using Microsoft.Data.SqlClient;

namespace SqlServerCoverage.Database
{
    internal static class DataReaderExtensions
    {
        public static int? GetIntOrNull(this SqlDataReader reader, string name)
        {
            if (reader.IsDBNull(name)) return null;
            return reader.GetInt32(name);
        }

        public static bool? GetBoolOrNull(this SqlDataReader reader, string name)
        {
            if (reader.IsDBNull(name)) return null;
            return reader.GetBoolean(name);
        }

        public static string GetStringOrNull(this SqlDataReader reader, int i)
        {
            if (reader.IsDBNull(i)) return null;
            return reader.GetString(i);
        }

        public static string GetStringOrNull(this SqlDataReader reader, string name)
        {
            if (reader.IsDBNull(name)) return null;
            return reader.GetString(name);
        }
    }
}
