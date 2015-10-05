using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace NServiceBus.SqlPersistence
{
    public class ScriptBuilderTask: Task
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
            var moduleDefinition = ModuleDefinition.ReadModule(TargetPath, readerParameters);
            var targetDirectory = Path.GetDirectoryName(TargetPath);
            var metaDataReader = new SagaMetaDataReader(moduleDefinition);
            foreach (var map in metaDataReader.GetSagaMaps())
            {
                //SagaScriptBuilder.BuildCreateScript();
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
