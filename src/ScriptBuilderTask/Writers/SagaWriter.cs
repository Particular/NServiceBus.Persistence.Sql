using System.IO;
using Mono.Cecil;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class SagaWriter
{
    public static void WriteSagaScripts(string scriptPath, ModuleDefinition moduleDefinition, BuildSqlVariant sqlVariant, BuildLogger buildLogger)
    {
        var metaDataReader = new AllSagaDefinitionReader(moduleDefinition);
        var sagasScriptPath = Path.Combine(scriptPath, "Sagas");
        Directory.CreateDirectory(sagasScriptPath);
        var index = 0;
        foreach (var saga in metaDataReader.GetSagas((exception, type) =>
        {
            buildLogger.LogError($"Error in '{type.FullName}'. Error:{exception.Message}", type.GetFileName());
        }))
        {
            var sagaFileName = saga.TableSuffix;
            var maximumNameLength = 244 - sagasScriptPath.Length;
            if (sagaFileName.Length > maximumNameLength)
            {
                sagaFileName = $"{sagaFileName.Substring(0, maximumNameLength)}_{index}";
                index++;
            }
            var createPath = Path.Combine(sagasScriptPath, $"{sagaFileName}_Create.sql");
            File.Delete(createPath);
            using (var writer = File.CreateText(createPath))
            {
                SagaScriptBuilder.BuildCreateScript(saga, sqlVariant, writer);
            }

            var dropPath = Path.Combine(sagasScriptPath, $"{sagaFileName}_Drop.sql");
            File.Delete(dropPath);
            using (var writer = File.CreateText(dropPath))
            {
                SagaScriptBuilder.BuildDropScript(saga, sqlVariant, writer);
            }
        }
    }
}