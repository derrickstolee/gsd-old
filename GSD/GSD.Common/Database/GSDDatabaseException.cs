using System;

namespace GSD.Common.Database
{
    public class GSDDatabaseException : Exception
    {
        public GSDDatabaseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
