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
        ScriptGenerator.Generate(assemblyPath, intermediateDirectory, logError, FindPromotionPath);
    }

    string FindPromotionPath(string promotionPath)
    {
        promotionPath = promotionPath
            .Replace("$(ProjectDir)", GetFullPathWithEndingSlashes(projectDirectory));

        if (!promotionPath.Contains(@"..\"))
        {
            return promotionPath;
        }

        if (string.IsNullOrWhiteSpace(solutionDirectory))
        {
            throw new ErrorsException(
                @"The ScriptPromotionPath contains '..\' but no SolutionDirectory was passed to the MSBuildTask. One possible cause of this is a csproj file is being build directly, rather than building the parent solution.
Possible workarounds:

 * Don't use '..\' in the ScriptPromotionPath
 * Build the solution rather than the project
 * Add a property to the project that adds the SolutionDir property: <PropertyGroup><SolutionDir Condition=""..\ == '' Or ..\ == '*Undefined*'"">..\</SolutionDir></PropertyGroup>");
        }

        promotionPath = promotionPath
            .Replace(@"..\", GetFullPathWithEndingSlashes(solutionDirectory));

        return promotionPath;
    }

    static string GetFullPathWithEndingSlashes(string input)
    {
        return input.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }
}