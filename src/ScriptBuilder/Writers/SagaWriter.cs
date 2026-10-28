#nullable enable

using System.Collections.Generic;
using System.IO;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class SagaWriter(bool clean,
    bool overwrite,
    string scriptPath,
    IReadOnlyCollection<SagaDefinition> sagaDefinitions)
    : ScriptWriter(clean, overwrite, scriptPath)
{
    public override void WriteScripts(BuildSqlDialect dialect)
    {
        Directory.CreateDirectory(sagaPath);

        var index = 0;
        foreach (var saga in sagaDefinitions)
        {
            var sagaFileName = saga.TableSuffix;
            var maximumNameLength = 244 - ScriptPath.Length;
            if (sagaFileName.Length > maximumNameLength)
            {
                sagaFileName = $"{sagaFileName[..maximumNameLength]}_{index}";
                index++;
            }

            var createPath = Path.Combine(SagaDirectoryName, $"{sagaFileName}_Create.sql");
            WriteScript(createPath, writer => SagaScriptBuilder.BuildCreateScript(saga, dialect, writer));

            var dropPath = Path.Combine(SagaDirectoryName, $"{sagaFileName}_Drop.sql");
            WriteScript(dropPath, writer => SagaScriptBuilder.BuildDropScript(saga, dialect, writer));
        }
    }

    const string SagaDirectoryName = "Sagas";

    readonly string sagaPath = Path.Combine(scriptPath, SagaDirectoryName);
}