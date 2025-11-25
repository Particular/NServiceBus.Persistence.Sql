#nullable enable

using System;
using System.IO;
using Mono.Cecil;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class SagaWriter(bool clean,
    bool overwrite,
    string scriptPath,
    ModuleDefinition moduleDefinition,
    Action<string, string>? logError = null)
    : ScriptWriter(clean, overwrite, scriptPath)
{
    public override void WriteScripts(BuildSqlDialect dialect)
    {
        Directory.CreateDirectory(sagaPath);

        var metaDataReader = new AllSagaDefinitionReader(moduleDefinition);

        var index = 0;
        foreach (var saga in metaDataReader.GetSagas(logError))
        {
            var sagaFileName = saga.TableSuffix;
            var maximumNameLength = 244 - ScriptPath.Length;
            if (sagaFileName.Length > maximumNameLength)
            {
                sagaFileName = $"{sagaFileName.Substring(0, maximumNameLength)}_{index}";
                index++;
            }

            var createPath = Path.Combine(sagaPath, $"{sagaFileName}_Create.sql");
            WriteScript(createPath, writer => SagaScriptBuilder.BuildCreateScript(saga, dialect, writer));

            var dropPath = Path.Combine(sagaPath, $"{sagaFileName}_Drop.sql");
            WriteScript(dropPath, writer => SagaScriptBuilder.BuildDropScript(saga, dialect, writer));
        }
    }

    readonly string sagaPath = Path.Combine(scriptPath, "Sagas");
}