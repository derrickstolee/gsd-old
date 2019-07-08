using System;

namespace GSD.Common.NamedPipes
{
    public class PipeNameLengthException : Exception
    {
        public PipeNameLengthException(string message)
            : base(message)
        {
        }
    }
}
