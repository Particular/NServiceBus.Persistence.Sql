using Microsoft.Build.Framework;

class BuildLogger
{
    IBuildEngine buildEngine;

    public BuildLogger(IBuildEngine buildEngine)
    {
        this.buildEngine = buildEngine;
    }

    public void LogInfo(string message)
    {
        buildEngine.LogMessageEvent(new BuildMessageEventArgs(PrependMessage(message), "", "SqlPersistenceTask", MessageImportance.Normal));
    }

    static string PrependMessage(string message)
    {
        return $"SqlPersistenceTask: {message}";
    }

    public void LogError(string message, string file = null)
    {
        ErrorOccurred = true;
        buildEngine.LogErrorEvent(new BuildErrorEventArgs("", "", file, 0, 0, 0, 0, PrependMessage(message), "", "SqlPersistenceTask"));
    }

    public bool ErrorOccurred;
}