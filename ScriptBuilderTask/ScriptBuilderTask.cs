using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace NServiceBus.Persistence.Sql
{
    public class ScriptBuilderTask:
#if (DEBUG)
        AppDomainIsolatedTask
#else
        Task
#endif
    {
        BuildLogger logger;

        [Required]
        public string AssemblyPath { get; set; }
        [Required]
        public string IntermediateDirectory { get; set; }

        public override bool Execute()
        {
            logger = new BuildLogger(BuildEngine);
            logger.LogInfo($"ScriptBuilderTask (version {typeof(ScriptBuilderTask).Assembly.GetName().Version}) Executing");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (!ValidateInputs())
                {
                    return false;
                }
                Inner();
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

        bool ValidateInputs()
        {
            if (!File.Exists(AssemblyPath))
            {
                logger.LogError($"AssemblyPath \"{AssemblyPath}\" does not exist.");
                return false;
            }

            if (!Directory.Exists(IntermediateDirectory))
            {
                logger.LogError($"IntermediateDirectory \"{IntermediateDirectory}\" does not exist.");
                return false;
            }
            return true;
        }

        void Inner()
        {
            var scriptPath = Path.Combine(IntermediateDirectory, "NServiceBus.Persistence.Sql");
            Directory.CreateDirectory(scriptPath);
            WriteSagaScripts(scriptPath);
            WriteTimeoutScript(scriptPath);
            WriteSubscriptionScript(scriptPath);
            WriteOutboxScript(scriptPath);
        }

        void WriteSagaScripts(string scriptPath)
        {
            var moduleDefinition = ModuleDefinition.ReadModule(AssemblyPath);
            var metaDataReader = new AllSagaDefinitionReader(moduleDefinition);
            var sagasScriptPath = Path.Combine(scriptPath, "Sagas");
            Directory.CreateDirectory(sagasScriptPath);
            var index = 0;
            foreach (var saga in metaDataReader.GetSagas((exception, type) =>
            {
                logger.LogError($"Error in '{type.FullName}'. Error:{exception.Message}", type.GetFileName());
            }))
            {
                var sagaFileName = saga.TableSuffix;
                var maximumNameLength = 244 - sagasScriptPath.Length;
                if (sagaFileName.Length > maximumNameLength)
                {
                    sagaFileName = sagaFileName.Substring(0, maximumNameLength) + "_" + index;
                    index++;
                }
                var createPath = Path.Combine(sagasScriptPath, sagaFileName + "_Create.sql");
                File.Delete(createPath);
                using (var writer = File.CreateText(createPath))
                {
                    SagaScriptBuilder.BuildCreateScript(saga, writer);
                }

                var dropPath = Path.Combine(sagasScriptPath, sagaFileName + "_Drop.sql");
                File.Delete(dropPath);
                using (var writer = File.CreateText(dropPath))
                {
                    SagaScriptBuilder.BuildDropScript(saga, writer);
                }
            }
        }

        static void WriteTimeoutScript(string scriptPath)
        {
            var createPath = Path.Combine(scriptPath, "Timeout_Create.sql");
            File.Delete(createPath);
            using (var writer = File.CreateText(createPath))
            {
                TimeoutScriptBuilder.BuildCreateScript(writer);
            }
            var dropPath = Path.Combine(scriptPath, "Timeout_Drop.sql");
            File.Delete(dropPath);
            using (var writer = File.CreateText(dropPath))
            {
                TimeoutScriptBuilder.BuildDropScript(writer);
            }
        }

        static void WriteOutboxScript(string scriptPath)
        {
            var createPath = Path.Combine(scriptPath, "Outbox_Create.sql");
            File.Delete(createPath);
            using (var writer = File.CreateText(createPath))
            {
                OutboxScriptBuilder.BuildCreateScript(writer);
            }
            var dropPath = Path.Combine(scriptPath, "Outbox_Drop.sql");
            File.Delete(dropPath);
            using (var writer = File.CreateText(dropPath))
            {
                OutboxScriptBuilder.BuildDropScript(writer);
            }
        }

        static void WriteSubscriptionScript(string scriptPath)
        {
            var createPath = Path.Combine(scriptPath, "Subscription_Create.sql");
            File.Delete(createPath);
            using (var writer = File.CreateText(createPath))
            {
                SubscriptionScriptBuilder.BuildCreateScript(writer);
            }
            var dropPath = Path.Combine(scriptPath, "Subscription_Drop.sql");
            File.Delete(dropPath);
            using (var writer = File.CreateText(dropPath))
            {
                SubscriptionScriptBuilder.BuildCreateScript(writer);
            }
        }

    }
}
