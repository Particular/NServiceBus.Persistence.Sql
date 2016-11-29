using System;
using System.Data.SqlClient;

namespace NServiceBus.Persistence.Sql
{
    public interface SqlPersistenceSerializer<TReader>
        where TReader : IDisposable
    {
        TReader GetReader(SqlDataReader reader, int column);

        void SetSerializeBuilder(SagaSerializeBuilder<TReader> defaultSagaSerialization);

        SagaSerializeBuilder<TReader> SerializationBuilder { get; }
        VersionDeserializeBuilder<TReader> VersionDeserializeBuilder { get; }

        void SetVersionDeserializeBuilder(VersionDeserializeBuilder<TReader> versionDeserializeBuilder);
    }
}