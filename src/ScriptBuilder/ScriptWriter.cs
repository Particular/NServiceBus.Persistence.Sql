
using System;
using System.IO;
using Mono.Cecil;

public static class ScriptWriter
{
    public static Settings Write(string assemblyPath, string targetDirectory, Action<string, string> logError)
    {
        var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters(ReadingMode.Deferred));
        var settings = SettingsAttributeReader.Read(module);
        foreach (var variant in settings.BuildVariants)
        {
            var variantPath = Path.Combine(targetDirectory, variant.ToString());
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
        return settings;
    }
}