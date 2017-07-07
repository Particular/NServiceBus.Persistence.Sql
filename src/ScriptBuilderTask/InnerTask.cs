using System;
using System.IO;
using Mono.Cecil;
using NServiceBus.Persistence.Sql;

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
        var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters(ReadingMode.Deferred));
        var scriptPath = Path.Combine(intermediateDirectory, "NServiceBus.Persistence.Sql");
        DirectoryExtensions.Delete(scriptPath);
        var settings = SettingsAttributeReader.Read(module);
        foreach (var variant in settings.BuildVariants)
        {
            var variantPath = Path.Combine(scriptPath, variant.ToString());
            Directory.CreateDirectory(variantPath);
            if (settings.ProduceSagaScripts)
            {
                SagaWriter.WriteSagaScripts(variantPath, module, variant, logError);
            }
            if (settings.ProduceTimeoutScripts)
            {
                TimeoutWriter.WriteTimeoutScript(variantPath, variant);
            }
            if (settings.ProduceSubscriptionScripts)
            {
                SubscriptionWriter.WriteSubscriptionScript(variantPath, variant);
            }
            if (settings.ProduceOutboxScripts)
            {
                OutboxWriter.WriteOutboxScript(variantPath, variant);
            }
        }

        PromoteFiles(scriptPath, settings);
    }

    void PromoteFiles(string scriptPath, Settings settings)
    {
        if (settings.ScriptPromotionPath==null)
        {
            return;
        }
        var replicationPath = settings.ScriptPromotionPath
            .Replace("$(ProjectDir)", projectDirectory)
            .Replace("$(SolutionDir)", solutionDirectory);
        try
        {
            DirectoryExtensions.Delete(replicationPath);
            DirectoryExtensions.DuplicateDirectory(scriptPath, replicationPath);
        }
        catch (Exception exception)
        {
            throw new ErrorsException($"Failed to promote scripts to '{replicationPath}'. Error: {exception.Message}");
        }
    }
}