using System;
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
        ScriptWriter.Write(assemblyPath, intermediateDirectory, logError, FindPromotionPath);
    }

    string FindPromotionPath(string promotionPathSetting)
    {
        return promotionPathSetting
            .Replace("$(ProjectDir)", projectDirectory)
            .Replace("$(SolutionDir)", solutionDirectory);
    }
}