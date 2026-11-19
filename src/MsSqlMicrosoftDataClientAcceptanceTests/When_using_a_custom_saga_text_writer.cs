using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

[TestFixture]
public class When_using_a_custom_saga_text_writer : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_store_the_json_written_into_the_persisters_writer()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<SagaEndpoint>(b => b
                .When(session => session.SendLocal(new StartSagaMessage { Property = "Test" })))
            .Done(c => c.HandledOtherMessage)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.TextWriterCreatorUsed, Is.True);
            Assert.That(context.ReceivedStringWriter, Is.False);
            Assert.That(context.LoadedProperty, Is.EqualTo("Test"));
            Assert.That(context.LoadedNote, Is.EqualTo(UppercaseNoteWriter.WrittenNote));
        }
    }

    [Test]
    public async Task Should_prefer_it_over_the_obsolete_overload()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithBothCreators>(b => b
                .When(session => session.SendLocal(new StartSagaMessage { Property = "Test" })))
            .Done(c => c.HandledOtherMessage)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.TextWriterCreatorUsed, Is.True);
            Assert.That(context.StringBuilderCreatorUsed, Is.False);
            Assert.That(context.LoadedNote, Is.EqualTo(UppercaseNoteWriter.WrittenNote));
        }
    }

    public class Context : ScenarioContext
    {
        public bool TextWriterCreatorUsed { get; set; }
        public bool StringBuilderCreatorUsed { get; set; }
        public bool ReceivedStringWriter { get; set; }
        public bool HandledOtherMessage { get; set; }
        public string LoadedProperty { get; set; }
        public string LoadedNote { get; set; }
    }

    public class SagaEndpoint : EndpointConfigurationBuilder
    {
        public SagaEndpoint() =>
            EndpointSetup<DefaultServer>((configuration, runDescriptor) =>
            {
                var testContext = (Context)runDescriptor.ScenarioContext;
                var persistence = configuration.UsePersistence<SqlPersistence>();
                persistence.SagaSettings().WriterCreator((TextWriter writer) =>
                {
                    testContext.TextWriterCreatorUsed = true;
                    testContext.ReceivedStringWriter = writer is StringWriter;
                    return new UppercaseNoteWriter(writer);
                });
            });

        public class TestSaga(Context testContext) : Saga<TestSaga.SagaData>,
            IAmStartedByMessages<StartSagaMessage>,
            IHandleMessages<SomeOtherMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                Data.Property = message.Property;
                Data.Note = UppercaseNoteWriter.OriginalNote;
                return context.SendLocal(new SomeOtherMessage { Property = message.Property });
            }

            public Task Handle(SomeOtherMessage message, IMessageHandlerContext context)
            {
                testContext.LoadedProperty = Data.Property;
                testContext.LoadedNote = Data.Note;
                testContext.HandledOtherMessage = true;
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
                mapper.MapSaga(s => s.Property)
                    .ToMessage<StartSagaMessage>(m => m.Property)
                    .ToMessage<SomeOtherMessage>(m => m.Property);

            public class SagaData : ContainSagaData
            {
                public string Property { get; set; }
                public string Note { get; set; }
            }
        }
    }

    public class EndpointWithBothCreators : EndpointConfigurationBuilder
    {
        public EndpointWithBothCreators() =>
            EndpointSetup<DefaultServer>((configuration, runDescriptor) =>
            {
                var testContext = (Context)runDescriptor.ScenarioContext;
                var sagaSettings = configuration.UsePersistence<SqlPersistence>().SagaSettings();
                sagaSettings.WriterCreator((TextWriter writer) =>
                {
                    testContext.TextWriterCreatorUsed = true;
                    return new UppercaseNoteWriter(writer);
                });
#pragma warning disable CS0618 // Configured after the TextWriter overload to show registration order doesn't matter
                sagaSettings.WriterCreator(builder =>
                {
                    testContext.StringBuilderCreatorUsed = true;
                    return new JsonTextWriter(new StringWriter(builder));
                });
#pragma warning restore CS0618
            });

        public class TestSaga(Context testContext) : Saga<TestSaga.SagaData>,
            IAmStartedByMessages<StartSagaMessage>,
            IHandleMessages<SomeOtherMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                Data.Property = message.Property;
                Data.Note = UppercaseNoteWriter.OriginalNote;
                return context.SendLocal(new SomeOtherMessage { Property = message.Property });
            }

            public Task Handle(SomeOtherMessage message, IMessageHandlerContext context)
            {
                testContext.LoadedNote = Data.Note;
                testContext.HandledOtherMessage = true;
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
                mapper.MapSaga(s => s.Property)
                    .ToMessage<StartSagaMessage>(m => m.Property)
                    .ToMessage<SomeOtherMessage>(m => m.Property);

            public class SagaData : ContainSagaData
            {
                public string Property { get; set; }
                public string Note { get; set; }
            }
        }
    }

    class UppercaseNoteWriter(TextWriter writer) : JsonTextWriter(writer)
    {
        public const string OriginalNote = "written by the saga";
        public const string WrittenNote = "WRITTEN BY THE SAGA";

        public override void WriteValue(string value) => base.WriteValue(value == OriginalNote ? WrittenNote : value);
    }

    public class StartSagaMessage : IMessage
    {
        public string Property { get; set; }
    }

    public class SomeOtherMessage : IMessage
    {
        public string Property { get; set; }
    }
}
