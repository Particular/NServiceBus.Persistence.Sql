namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    using System;
    using System.IO;
    using Mono.Cecil;

    public static class ScriptWriter
    {
        public static void Write(string assemblyPath, string targetDirectory, Action<string, string> logError, Func<string,string> promotionPathFinder)
        {
            var scriptPath = Path.Combine(targetDirectory, "NServiceBus.Persistence.Sql");
            PurgeDialectDirs(scriptPath);
            Directory.CreateDirectory(scriptPath);
            Settings settings;
            using (var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters(ReadingMode.Deferred)))
            {
                settings = SettingsAttributeReader.Read(module);
                foreach (var dialect in settings.BuildDialects)
                {
                    var dialectPath = Path.Combine(scriptPath, dialect.ToString());
                    Directory.CreateDirectory(dialectPath);
                    if (settings.ProduceSagaScripts)
                    {
                        SagaWriter.WriteSagaScripts(dialectPath, module, dialect, logError);
                    }
                    if (settings.ProduceTimeoutScripts)
                    {
                        TimeoutWriter.WriteTimeoutScript(dialectPath, dialect);
                    }
                    if (settings.ProduceSubscriptionScripts)
                    {
                        SubscriptionWriter.WriteSubscriptionScript(dialectPath, dialect);
                    }
                    if (settings.ProduceOutboxScripts)
                    {
                        OutboxWriter.WriteOutboxScript(dialectPath, dialect);
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

        static void PurgeDialectDirs(string scriptPath)
        {
            foreach (var dialect in Enum.GetNames(typeof(BuildSqlDialect)))
            {
                var dialectDirectory = Path.Combine(scriptPath, dialect);
                DirectoryExtensions.Delete(dialectDirectory);
            }
        }

        static void Promote(string replicationPath, string scriptPath)
        {
            try
            {
                PurgeDialectDirs(replicationPath);
                DirectoryExtensions.DuplicateDirectory(scriptPath, replicationPath);
            }
            catch (Exception exception)
            {
                throw new ErrorsException($"Failed to promote scripts to '{replicationPath}'. Error: {exception.Message}");
            }
        }
    }
}