using System;
using System.IO;
using Mono.Cecil;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class InnerTask
{
    string assemblyPath;
    string intermediateDirectory;
    string projectDirectory;
    string solutionDirectory;
    Action<string, string> logError;

    public InnerTask(string assemblyPath, string intermediateDirectory, string projectDirectory, string solutionDirectory, Action<string, string> logError)
    {
        this.assemblyPath = assemblyPath;
        this.intermediateDirectory = intermediateDirectory;
        this.projectDirectory = projectDirectory;
        this.solutionDirectory = solutionDirectory;
        this.logError = logError;
    }

    public void Execute()
    {
        var moduleDefinition = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters(ReadingMode.Deferred));
        var scriptPath = Path.Combine(intermediateDirectory, "NServiceBus.Persistence.Sql");
        DirectoryExtentions.Delete(scriptPath);
        foreach (var variant in SqlVariantReader.Read(moduleDefinition))
        {
            var variantPath = Path.Combine(scriptPath, variant.ToString());
            Write(moduleDefinition, variant, variantPath);
        }

        PromoteFiles(moduleDefinition, scriptPath);
    }

    void PromoteFiles(ModuleDefinition moduleDefinition, string scriptPath)
    {
        string customPath;
        if (!ScriptPromotionPathReader.TryRead(moduleDefinition, out customPath))
        {
            return;
        }
        var replicationPath = customPath
            .Replace("$(ProjectDir)", projectDirectory)
            .Replace("$(SolutionDir)", solutionDirectory);
        try
        {
            DirectoryExtentions.Delete(replicationPath);
            DirectoryExtentions.DuplicateDirectory(scriptPath, replicationPath);
        }
        catch (Exception exception)
        {
            throw new ErrorsException($"Failed to promote scripts to '{replicationPath}'. Error: {exception.Message}");
        }
    }

    void Write(ModuleDefinition moduleDefinition, BuildSqlVariant sqlVariant, string scriptPath)
    {
        Directory.CreateDirectory(scriptPath);
        SagaWriter.WriteSagaScripts(scriptPath, moduleDefinition, sqlVariant, logError);
        TimeoutWriter.WriteTimeoutScript(scriptPath, sqlVariant);
        SubscriptionWriter.WriteSubscriptionScript(scriptPath, sqlVariant);
        OutboxWriter.WriteOutboxScript(scriptPath, sqlVariant);
    }
}