using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

class BuildLogger
{

    TaskLoggingHelper loggingHelper;

    public BuildLogger(TaskLoggingHelper loggingHelper)
    {
         this.loggingHelper = loggingHelper;
    }

    public void LogInfo(string message)
    {
        loggingHelper.LogMessageFromText(message, MessageImportance.Normal);
    }

    public void LogError(string message,string file)
    {
        loggingHelper.LogError("", "", file, 0, 0, 0, 0, message);
    }
    public void LogError(string message)
    {
        loggingHelper.LogError("", "", null, 0, 0, 0, 0, message);
    }
}