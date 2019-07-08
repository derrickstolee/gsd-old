using System;

namespace GSD.Common
{
    public class FileBasedCollectionException : Exception
    {
        public FileBasedCollectionException(Exception innerException)
            : base(innerException.Message, innerException)
        {
        }
    }
}
