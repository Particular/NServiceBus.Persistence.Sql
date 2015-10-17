using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace NServiceBus.SqlPersistence
{
    public class ScriptBuilderTask:
#if (DEBUG)
        AppDomainIsolatedTask
#else        
        Task
#endif

    {
        [Required]
        public string TargetPath { get; set; }

        [Required]
        public string References { get; set; }

        public override bool Execute()
        {
            Log.LogMessageFromText($"ScriptBuilderTask (version {typeof(ScriptBuilderTask).Assembly.GetName().Version}) Executing",MessageImportance.Normal);

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                Inner();
                return true;
            }
            catch (ErrorsException exception)
            {
                Log.LogErrorFromException(exception, true, true, exception.FileName);
                return false;
            }
            catch (Exception exception)
            {
                Log.LogErrorFromException(exception, true, true, null);
                return false;
            }
            finally
            {
                Log.LogMessageFromText($"  Finished ScriptBuilderTask {stopwatch.ElapsedMilliseconds}ms.",MessageImportance.Normal);
            }
        }

        void Inner()
        {
            ValidateTargetPath();

            var assemblyResolver = new AssemblyResolver(s => Log.LogMessageFromText(s,MessageImportance.Normal), References
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            
            var readerParameters = new ReaderParameters
            {
              AssemblyResolver  = assemblyResolver
            };
            var targetDirectory = Path.GetDirectoryName(TargetPath);

            var scriptPath = Path.Combine(targetDirectory, "NServiceBus.Persistence.Sql");
            Directory.CreateDirectory(scriptPath);
            WriteSagaScripts(readerParameters, scriptPath);
            WriteTimeoutScript(scriptPath);
            WriteSubscriptionScript(scriptPath);
        }

        void WriteSagaScripts(ReaderParameters readerParameters, string scriptPath)
        {
            var moduleDefinition = ModuleDefinition.ReadModule(TargetPath, readerParameters);
            var metaDataReader = new SagaMetaDataReader(moduleDefinition, new BuildLogger(Log));
            var sagasScriptPath = Path.Combine(scriptPath, "Sagas");
            Directory.CreateDirectory(sagasScriptPath);
            foreach (var saga in metaDataReader.GetSagas())
            {
                var createPath = Path.Combine(sagasScriptPath, saga.Name + "_Create.sql");
                File.Delete(createPath);
                using (var writer = File.CreateText(createPath))
                {
                    SagaScriptBuilder.BuildCreateScript(saga, writer);
                }
                var dropPath = Path.Combine(sagasScriptPath, saga.Name + "_Drop.sql");
                File.Delete(dropPath);
                using (var writer = File.CreateText(dropPath))
                {
                    SagaScriptBuilder.BuildDropScript(saga.Name, writer);
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

        void ValidateTargetPath()
        {
            if (!File.Exists(TargetPath))
            {
                throw new ErrorsException($"TargetPath \"{TargetPath}\" does not exist.");
            }
        }
    }
}
