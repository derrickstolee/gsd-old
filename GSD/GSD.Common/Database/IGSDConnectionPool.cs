using System.Data;

namespace GSD.Common.Database
{
    /// <summary>
    /// Interface for getting a pooled database connection
    /// </summary>
    public interface IGSDConnectionPool
    {
        IDbConnection GetConnection();
    }
}
