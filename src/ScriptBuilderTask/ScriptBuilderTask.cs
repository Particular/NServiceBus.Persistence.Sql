using System;
using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NServiceBus.Persistence.Sql
{
    using System.IO;

    public class ScriptBuilderTask : Task
    {
        BuildLogger logger;

        [Required]
        public string AssemblyPath { get; set; }

        [Required]
        public string IntermediateDirectory { get; set; }

        [Required]
        public string ProjectDirectory { get; set; }

        [Required]
        public string SolutionDirectory { get; set; }

        public override bool Execute()
        {
            logger = new BuildLogger(BuildEngine);
            logger.LogInfo($"ScriptBuilderTask (version {typeof(ScriptBuilderTask).Assembly.GetName().Version}) Executing");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                ValidateInputs();
                Action<string, string> logError = (error, file) =>
                {
                    logger.LogError(error, file);
                };
                var innerTask = new InnerTask(AssemblyPath, IntermediateDirectory, ProjectDirectory, SolutionDirectory, logError);
                innerTask.Execute();
            }
            catch (ErrorsException exception)
            {
                logger.LogError(exception.Message, exception.FileName);
            }
            catch (Exception exception)
            {
                logger.LogError(exception.ToFriendlyString());
            }
            finally
            {
                logger.LogInfo($"  Finished ScriptBuilderTask {stopwatch.ElapsedMilliseconds}ms.");
            }
            return !logger.ErrorOccurred;
        }

        void ValidateInputs()
        {
            if (!File.Exists(AssemblyPath))
            {
                throw new ErrorsException($"AssemblyPath '{AssemblyPath}' does not exist.");
            }

            if (!Directory.Exists(IntermediateDirectory))
            {
                throw new ErrorsException($"IntermediateDirectory '{IntermediateDirectory}' does not exist.");
            }

            if (!Directory.Exists(ProjectDirectory))
            {
                throw new ErrorsException($"ProjectDirectory '{ProjectDirectory}' does not exist.");
            }

            if (!Directory.Exists(SolutionDirectory))
            {
                throw new ErrorsException($"SolutionDirectory '{SolutionDirectory}' does not exist.");
            }
        }
    }
}