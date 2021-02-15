namespace NServiceBus.Persistence.Sql.CommandLine
{
    using System;
    using System.Collections.Generic;
    using McMaster.Extensions.CommandLineUtils;
    using ScriptBuilder;

    class Program
    {
        internal const string AppName = "sql-persistence";

        static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = AppName
            };

            var verboseOption = app.Option<bool>("-v | --verbose", "Verbose logging", CommandOptionType.NoValue, inherited: true);

            app.HelpOption(inherited: true);

            app.Command("script", cmd =>
            {
                var assemblyPath = cmd.Argument("assembly", "File path to the endpoint assembly.").IsRequired();
                var outputOption = cmd.Option("-o | --output-dir", "Path to the output directory.", CommandOptionType.SingleOrNoValue);
                var cleanOption = cmd.Option<bool>("--clean", "Removes existing files in the output directory.", CommandOptionType.SingleOrNoValue);
                var overwriteOption = cmd.Option<bool>("--overwrite", "Overwrites existing files in the output if they match the files to be generated.", CommandOptionType.SingleOrNoValue);
                var dialectOption = cmd.Option<DialectTypes>("--dialect", "Specifies a dialect to generate", CommandOptionType.SingleOrNoValue);

                cmd.OnExecuteAsync(async ct =>
                {
                    await Generator.Run(assemblyPath, outputOption, overwriteOption, cleanOption, dialectOption);
                });
            });

            app.OnExecute(() =>
            {
                Console.WriteLine("Specify a subcommand");
                app.ShowHelp();
                return 1;
            });

            try
            {
                return app.Execute(args);
            }
            catch (Exception exception)
            {
                var error = exception.ToFriendlyString();
                Console.Error.WriteLine($"{AppName} failed: {exception.Message}");

                if (verboseOption.HasValue())
                {
                    Console.Error.WriteLine(error);
                }
                return 1;
            }
        }
    }
}