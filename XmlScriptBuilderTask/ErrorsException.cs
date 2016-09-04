using System;

namespace NServiceBus.Persistence.Sql.Xml
{
    public class ErrorsException : Exception
    {
        public string FileName { get; set; }
        public ErrorsException(string message) : base(message)
        {
        }
    }
}