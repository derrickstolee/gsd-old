using System;

namespace GSD.Common
{
    public class InvalidRepoException : Exception
    {
        public InvalidRepoException(string message)
            : base(message)
        {
        }
    }
}
