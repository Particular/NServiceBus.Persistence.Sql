#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

public class ScriptGenerator(string assemblyPath,
    string destinationDirectory,
    bool clean = true,
    bool overwrite = true,
    IReadOnlyList<BuildSqlDialect>? dialectOptions = null,
    Func<string, string>? promotionFinder = null,
    Action<string, string>? logError = null)
{
    public static void Generate(string assemblyPath, string targetDirectory,
        Action<string, string> logError, Func<string, string> promotionPathFinder)
    {
        var writer = new ScriptGenerator(assemblyPath, targetDirectory, promotionFinder: promotionPathFinder, logError: logError);
        writer.Generate();
    }

    public void Generate()
    {
        if (clean)
        {
            PurgeDialectDirs(scriptBasePath);
        }

        CreateDirectories();

        Settings settings;
        using (var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters(ReadingMode.Deferred)))
        {
            settings = SettingsAttributeReader.Read(module);
            foreach (var dialect in settings.BuildDialects)
            {
                if (!ShouldGenerateDialect(dialect))
                {
                    continue;
                }

                var dialectPath = Path.Combine(scriptBasePath, dialect.ToString());

                CreateDialectDirectory(dialectPath);

                if (settings.ProduceSagaScripts)
                {
                    new SagaWriter(clean, overwrite, dialectPath, module, logError).WriteScripts(dialect);
                }

                if (settings.ProduceTimeoutScripts)
                {
                    new TimeoutWriter(clean, overwrite, dialectPath).WriteScripts(dialect);
                }

                if (settings.ProduceSubscriptionScripts)
                {
                    new SubscriptionWriter(clean, overwrite, dialectPath).WriteScripts(dialect);
                }

                if (settings.ProduceOutboxScripts)
                {
                    new OutboxWriter(clean, overwrite, dialectPath).WriteScripts(dialect);
                }
            }
        }

        var scriptPromotionPath = settings.ScriptPromotionPath;
        if (scriptPromotionPath == null || promotionFinder == null)
        {
            return;
        }

        var replicationPath = promotionFinder(scriptPromotionPath);
        Promote(replicationPath);
    }

    bool ShouldGenerateDialect(BuildSqlDialect dialect) => dialectOptions.Count == 0 || dialectOptions.Contains(dialect);

    void CreateDialectDirectory(string dialectPath) => Directory.CreateDirectory(dialectPath);

    void CreateDirectories() => Directory.CreateDirectory(scriptBasePath);

    void PurgeDialectDirs(string scriptPath)
    {
        foreach (var dialect in GetSelectedDialects())
        {
            var dialectDirectory = Path.Combine(scriptPath, dialect);
            if (!Directory.Exists(dialectDirectory))
            {
                continue;
            }

            foreach (var file in GetKnownScripts(dialectDirectory))
            {
                File.Delete(file);
            }

            var sagaDirectory = Path.Combine(dialectDirectory, "Sagas");
            if (!Directory.Exists(sagaDirectory))
            {
                continue;
            }

            foreach (var file in GetKnownScripts(sagaDirectory))
            {
                File.Delete(file);
            }
        }
    }

    IEnumerable<string> GetSelectedDialects() => dialectOptions.Count == 0 ? Enum.GetNames(typeof(BuildSqlDialect)) : dialectOptions.Select(d => d.ToString());

    static IEnumerable<string> GetKnownScripts(string dialectDirectory) =>
        Directory.EnumerateFiles(dialectDirectory, "*_Drop.sql")
            .Concat(Directory.EnumerateFiles(dialectDirectory, "*_Create.sql"));

    void Promote(string replicationPath)
    {
        if (clean)
        {
            PurgeDialectDirs(replicationPath);
        }

        try
        {
            DirectoryExtensions.DuplicateDirectory(scriptBasePath, replicationPath);
        }
        catch (Exception exception)
        {
            throw new ErrorsException($"Failed to promote scripts to '{replicationPath}'. Error: {exception.Message}");
        }
    }

    readonly string scriptBasePath = Path.Combine(destinationDirectory, "NServiceBus.Persistence.Sql");
    readonly IReadOnlyList<BuildSqlDialect> dialectOptions = dialectOptions ?? [];
}