using System;

namespace NServiceBus.Persistence.SqlServerXml
{
    public class ErrorsException : Exception
    {
        public string FileName { get; set; }
        public ErrorsException(string message) : base(message)
        {
        }
    }
}