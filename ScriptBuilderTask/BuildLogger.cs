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

    public void LogError(FileError fileError)
    {
        loggingHelper.LogError("", "", fileError.File, 0, 0, 0, 0, fileError.Message);
    }
}