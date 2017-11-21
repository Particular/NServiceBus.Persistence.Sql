using System;
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
        ScriptWriter.Write(assemblyPath, intermediateDirectory, logError, FindPromotionPath);
    }

    string FindPromotionPath(string promotionPath)
    {
        promotionPath = promotionPath
            .Replace("$(ProjectDir)", projectDirectory);

        if (!promotionPath.Contains("$(SolutionDir)"))
        {
            return promotionPath;
        }

        if (string.IsNullOrWhiteSpace(solutionDirectory))
        {
            throw new ErrorsException(
                @"The ScriptPromotionPath contains '$(SolutionDir)' but no SolutionDirectory was passed to the MSBuildTask. One possible cause of this is a csproj file is being build directly, rather than building the parent solution.
Possible workarounds:

 * Don't use '$(SolutionDir)' in the ScriptPromotionPath
 * Build the solution rather than the project
 * Add a property to the project that adds the SolutionDir property: <PropertyGroup><SolutionDir Condition=""$(SolutionDir) == '' Or $(SolutionDir) == '*Undefined*'"">..\</SolutionDir></PropertyGroup>");
        }

        promotionPath = promotionPath
            .Replace("$(SolutionDir)", solutionDirectory);

        return promotionPath;
    }
}