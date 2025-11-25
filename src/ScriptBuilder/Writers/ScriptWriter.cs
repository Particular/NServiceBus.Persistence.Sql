using System;
using System.IO;
using NServiceBus.Persistence.Sql.ScriptBuilder;

public abstract class ScriptWriter(bool clean, bool overwrite, string scriptPath)
{
    protected bool Clean { get; } = clean;

    protected bool Overwrite { get; } = overwrite;

    protected string ScriptPath { get; } = scriptPath;

    public abstract void WriteScripts(BuildSqlDialect dialect);

    protected void WriteScript(string fileName, Action<StreamWriter> action)
    {
        var filePath = EnsureFile(fileName);

        using var writer = File.CreateText(filePath);
        action(writer);
    }

    string EnsureFile(string file)
    {
        var filePath = Path.Combine(ScriptPath, file);
        if (Clean && File.Exists(file))
        {
            File.Delete(filePath);
        }

        if (!Overwrite && File.Exists(filePath))
        {
            throw new Exception($"File '{filePath}' already exists.");
        }

        return filePath;
    }
}