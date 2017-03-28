using System;

namespace NServiceBus.Persistence.Sql
{
    class SerializationException : Exception
    {
        public SerializationException(Exception innerException) : base("Serialization failed", innerException)
        {
        }
    }
}