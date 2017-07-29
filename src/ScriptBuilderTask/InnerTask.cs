using System;
using System.IO;
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
        var scriptPath = Path.Combine(intermediateDirectory, "NServiceBus.Persistence.Sql");
        DirectoryExtensions.Delete(scriptPath);
        var settings = ScriptWriter.Write(assemblyPath, scriptPath, logError);

        PromoteFiles(scriptPath, settings);
    }

    void PromoteFiles(string scriptPath, Settings settings)
    {
        if (settings.ScriptPromotionPath == null)
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