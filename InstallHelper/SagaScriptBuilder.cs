using System;
using System.Collections.Generic;
using System.IO;

namespace NServiceBus.SqlPersistence
{
    public static class SagaScriptBuilder
    {

        public static void BuildCreateScript(string schema, string endpointName, IEnumerable<SagaDefinition> sagas, Func<string, TextWriter> writerBuilder)
        {
            foreach (var saga in sagas)
            {
                var writer = writerBuilder(saga.Name);
                writer.Write(@"
IF NOT  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[{1}.{2}]') AND type in (N'U'))
BEGIN
    CREATE TABLE [{0}].[{1}.{2}](
	    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
	    [Originator] [nvarchar](255) NULL,
	    [OriginalMessageId] [nvarchar](255) NULL,
	    [Data] [xml] NOT NULL
    )
END
", schema, endpointName, saga.Name);
            }
        }

        public static void BuildDropScript(string schema, string endpointName, IEnumerable<SagaDefinition> sagas, Func<string, TextWriter> writerBuilder)
        {
            foreach (var saga in sagas)
            {
                var writer = writerBuilder(saga.Name);
                writer.Write(@"
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[{1}.{2}]') AND type in (N'U'))
BEGIN
    DROP TABLE [{0}].[{1}.{2}]
END
", schema, endpointName, saga.Name);
            }
        }

    }
}