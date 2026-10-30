using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Sagas;
using NUnit.Framework;

[TestFixture]
public class SqlTimeoutPersisterTests
{
    static IEnumerable<SqlDialect> Dialects() =>
    [
        new SqlDialect.MsSqlServer(),
        new SqlDialect.MySql(),
        new SqlDialect.Oracle(),
        new SqlDialect.PostgreSql()
    ];

    [TestCaseSource(nameof(Dialects))]
    public void Commands_are_scoped_by_saga_and_timeout_id(SqlDialect dialect)
    {
        var tableName = dialect.GetTimeoutTableName("Prefix_");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tableName, Does.Contain("Prefix_").IgnoreCase);
            // The legacy timeout manager table (TimeoutData, or TO on Oracle) must never be used.
            Assert.That(tableName, dialect is SqlDialect.Oracle ? Does.EndWith("ST\"") : Does.Contain("SagaTimeoutData"));
            foreach (var command in new[]
            {
                dialect.GetTimeoutGetCommand(tableName),
                dialect.GetTimeoutRequestCommand(tableName),
                dialect.GetTimeoutRescheduleCommand(tableName),
                dialect.GetTimeoutRemoveCommand(tableName),
                dialect.GetTimeoutRemoveBySagaIdCommand(tableName)
            })
            {
                Assert.That(command, Does.Contain(tableName).And.Contain("SagaId"));
            }
            Assert.That(dialect.GetTimeoutRescheduleCommand(tableName), Does.Contain("TimeoutId"));
            Assert.That(dialect.GetTimeoutRemoveCommand(tableName), Does.Contain("TimeoutId"));
            Assert.That(dialect.GetTimeoutRemoveBySagaIdCommand(tableName), Does.Not.Contain("TimeoutId"));
        }
    }

    [Test]
    public void Serialization_preserves_every_field()
    {
        var timeout = new StoredSagaTimeout
        {
            TimeoutId = "id \"with\" 'quotes' ✓",
            TriggerId = "trigger",
            Version = 3,
            DueTime = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.FromHours(-5.5)).AddTicks(1234567),
            MessageType = "My.Namespace.Type, Assembly ✓",
            ContentType = "application/json; charset=utf-8",
            Payload = [0, 1, 2, 255]
        };

        var roundTripped = SqlTimeoutPersister.Deserialize(SqlTimeoutPersister.Serialize(timeout));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(roundTripped.TimeoutId, Is.EqualTo(timeout.TimeoutId));
            Assert.That(roundTripped.TriggerId, Is.EqualTo(timeout.TriggerId));
            Assert.That(roundTripped.Version, Is.EqualTo(timeout.Version));
            Assert.That(roundTripped.DueTime, Is.EqualTo(timeout.DueTime));
            Assert.That(roundTripped.DueTime.Offset, Is.EqualTo(timeout.DueTime.Offset));
            Assert.That(roundTripped.DueTime.Ticks, Is.EqualTo(timeout.DueTime.Ticks));
            Assert.That(roundTripped.MessageType, Is.EqualTo(timeout.MessageType));
            Assert.That(roundTripped.ContentType, Is.EqualTo(timeout.ContentType));
            Assert.That(roundTripped.Payload, Is.EqualTo(timeout.Payload));
        }
    }
}
