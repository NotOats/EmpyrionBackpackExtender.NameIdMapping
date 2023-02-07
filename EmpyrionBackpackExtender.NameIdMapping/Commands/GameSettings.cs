using EmpyrionBackpackExtender.NameIdMapping.GameFiles;
using Spectre.Console.Cli;
using Spectre.Console;
using System.ComponentModel;
using System.IO.Abstractions;

namespace EmpyrionBackpackExtender.NameIdMapping.Commands;

internal class GameSettings : CommandSettings
{
    [CommandOption("--folder <server_folder>")]
    [Description("The server's root folder (where 'BuildNumber.txt' is located)")]
    public string? ServerFolder { get; set; }

    [CommandOption("--config <configuration_file>")]
    [Description("The server's configuration file.")]
    public string? ServerConfig { get; set; }

    [CommandOption("--ecf <ecf_files>")]
    [Description("A comma seperated string of ecf files to read Item & Blocks from")]
    public string? EcfFiles { get; set; }

    public void PromptForMissing(IAnsiConsole console, IFileSystem fileSystem)
    {
        ServerFolder ??= Ask(console, "What is your server's [green]root folder[/] (where 'BuildNumber.txt' is located)?");
        ServerConfig ??= Ask(console, "What is your server's [green]configuration file[/]?", "dedicated.yaml");

        // Pull available list for selection
        if (EcfFiles == null)
        {
            var save = new SaveGame(fileSystem, ServerFolder, ServerConfig);
            string[] options = save.ScenarioEcfFiles().Select(Path.GetFileName).Where(file => file != null).ToArray()!;
            var defaults = new[] { "BlocksConfig.ecf", "ItemsConfig.ecf" }.Where(options.Contains);

            var files = console.Prompt(
                new MultiSelectionPrompt<string>()
                    .PageSize(10)
                    .Title("Which ECF Files should should I use?")
                    .MoreChoicesText("[gray](Move up and down to reveal more files)[/]")
                    .InstructionsText("[grey](Press [blue]space[/] to toggle a file, [green]enter[/] to accept)[/]")
                    .AddChoiceGroup("Defaults", defaults)
                    .AddChoices(options.Where(file => !defaults.Contains(file)))
                );

            EcfFiles = string.Join(',', files);
        }
    }

    private static string Ask(IAnsiConsole console, string prompt)
    {
        return console.Ask<string>(prompt);
    }

    private static string Ask(IAnsiConsole console, string prompt, string defaultValue)
    {
        return new TextPrompt<string>(prompt)
            .DefaultValue(defaultValue)
            .Show(console);
    }
}
