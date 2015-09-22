using System.Collections.Generic;
using System.Text;

namespace NServiceBus.SqlPersistence
{
    public static class SagaScriptBuilder
    {
        public static string Build(string schema, IEnumerable<SagaDefinition> sagas)
        {
            var stringBuilder = new StringBuilder();
            foreach (var saga in sagas)
            {
                stringBuilder.AppendFormat(@"
IF NOT  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[{1}]') AND type in (N'U'))
BEGIN
    CREATE TABLE [{0}].[{1}](
	    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
	    [Originator] [nvarchar](255) NULL,
	    [OriginalMessageId] [nvarchar](255) NULL,
	    [Data] [xml] NOT NULL
    )
END
", schema, saga.Name);
            }
            return stringBuilder.ToString();
        }

    }
}