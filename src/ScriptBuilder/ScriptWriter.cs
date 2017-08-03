using System;
using System.IO;
using Mono.Cecil;

namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    public static class ScriptWriter
    {
        public static void Write(string assemblyPath, string targetDirectory, Action<string, string> logError, Func<string,string> promotionPathFinder)
        {
            var scriptPath = Path.Combine(targetDirectory, "NServiceBus.Persistence.Sql");
            DirectoryExtensions.Delete(scriptPath);
            Directory.CreateDirectory(scriptPath);
            Settings settings;
            using (var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters(ReadingMode.Deferred)))
            {
                settings = SettingsAttributeReader.Read(module);
                foreach (var variant in settings.BuildVariants)
                {
                    var variantPath = Path.Combine(scriptPath, variant.ToString());
                    Directory.CreateDirectory(variantPath);
                    if (settings.ProduceSagaScripts)
                    {
                        SagaWriter.WriteSagaScripts(variantPath, module, variant, logError);
                    }
                    if (settings.ProduceTimeoutScripts)
                    {
                        TimeoutWriter.WriteTimeoutScript(variantPath, variant);
                    }
                    if (settings.ProduceSubscriptionScripts)
                    {
                        SubscriptionWriter.WriteSubscriptionScript(variantPath, variant);
                    }
                    if (settings.ProduceOutboxScripts)
                    {
                        OutboxWriter.WriteOutboxScript(variantPath, variant);
                    }
                }
            }

            var scriptPromotionPath = settings.ScriptPromotionPath;
            if (scriptPromotionPath == null)
            {
                return;
            }
            var replicationPath = promotionPathFinder(scriptPromotionPath);
            Promote(replicationPath, scriptPath);
        }

        static void Promote(string replicationPath, string scriptPath)
        {
            try
            {
                DirectoryExtensions.Delete(replicationPath);
                DirectoryExtensions.DuplicateDirectory(scriptPath, replicationPath);
            }
            catch (Exception exception)
            {
                throw new ErrorsException($"Failed to promote scripts to '{replicationPath}'. Error: {exception.Message}");
            }
        }
    }
}