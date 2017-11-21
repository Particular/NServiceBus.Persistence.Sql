namespace NServiceBus.Persistence.Sql
{
    using System;

    public class ErrorsException : Exception
    {
        public string FileName { get; set; }
        public ErrorsException(string message) : base(message)
        {
        }
    }
}