using System;

namespace NServiceBus.SqlPersistence
{
    public class ErrorsException : Exception
    {
        public string FileName { get; set; }
        public ErrorsException(string message) : base(message)
        {
        }
    }
}