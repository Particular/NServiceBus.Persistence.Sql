namespace NServiceBus.Persistence.Sql.CommandLine
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using McMaster.Extensions.CommandLineUtils;

    static class Generator
    {
        public static Task Run(
            CommandArgument assemblyPath,
            CommandOption outputPath,
            CommandOption<bool> overwriteOption,
            CommandOption<bool> cleanOption,
            CommandOption<DialectTypes> dialectOption)
        {
            var output = outputPath.HasValue() ? outputPath.Value() : Directory.GetCurrentDirectory();

            var writer = new ScriptGenerator(assemblyPath.Value, output,
                cleanOption.ParsedValue, overwriteOption.ParsedValue,
                dialectOption.ParsedValues.ToBuildSqlDialects());

            writer.Generate();

            Console.WriteLine($"Script for {assemblyPath.Value} was generated at {output}.");
            return Task.CompletedTask;
        }
    }
}