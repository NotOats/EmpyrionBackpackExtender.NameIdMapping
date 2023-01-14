using EmpyrionBackpackExtender.NameIdMapping.GameFiles;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

AnsiConsole.Write(new FigletText("NameIdMapping Generator").Centered());

var app = new CommandApp<DumpNameIdMappingFile>();
return app.Run(args);

class GameSettings : CommandSettings
{
    [CommandArgument(0, "[server_folder]")]
    [Description("The server's root folder (where 'BuildNumber.txt' is located)")]
    public string? ServerFolder { get; set; }

    [CommandArgument(1, "[server_config]")]
    [Description("The server's configuration file (typically dedicated.yaml)")]
    public string? ServerConfig { get; set; }


    [CommandArgument(2, "[ecf_files]")]
    [Description("A List of ecf files to read Item & Blocks from")]
    public string[]? EcfFiles { get; set; }

    public void PromptForMissing()
    {
        ServerFolder ??= AnsiConsole.Ask<string>("What is your server's [green]root folder[/] (where 'BuildNumber.txt' is located)?");

        ServerConfig ??= AnsiConsole.Ask<string>("What is your server's [green]configuration file[/]?", "dedicated.yaml");

        // Pull available list for selection
        if (EcfFiles == null || EcfFiles.Length == 0)
        {
            var save = new SaveGame(ServerFolder, ServerConfig);
            string[] options = save.ScenarioEcfFiles().Select(Path.GetFileName).Where(file => file != null).ToArray()!;
            var defaults = new[] { "Config_RE.ecf", "BlocksConfig.ecf", "ItemsConfig.ecf" }.Where(options.Contains);

            var files = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .PageSize(10)
                    .Title("Which ECF Files should should I use?")
                    .MoreChoicesText("[gray](Move up and down to reveal more files)[/]")
                    .InstructionsText("[grey](Press [blue]space[/] to toggle a file, [green]enter[/] to accept)[/]")
                    .AddChoiceGroup("Defaults", defaults)
                    .AddChoices(options.Where(file => !defaults.Contains(file)))
                );

            EcfFiles = files.ToArray();
        }
    }
}

class DumpNameIdMappingFile : Command<GameSettings>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] GameSettings settings)
    {
        // Prompt for missing settings
        settings.PromptForMissing();

        // Load game files
        var save = new SaveGame(settings.ServerFolder!, settings.ServerConfig!);

        // Dump save info
        var uiPaths = new Rows(
            new Text($"Root Folder: {save.ServerDirectory}"),
            new Text($"Game Name:   {save.ServerConfig.GameName}"),
            new Text($"Scenario:    {save.ServerConfig.CustomScenario}"));

        AnsiConsole.Write(new Panel(uiPaths).Header("Server Information").Expand());

        // Timer
        var sw = Stopwatch.StartNew();

        // Do Map things
        var map = save.CreateRealIdToNameMap(settings.EcfFiles!);
        AnsiConsole.WriteLine($"Generated Real Id <-> Name map with {map.Count} entries");

        // Change to the correct dictionary found in GitHub-TC/EmpyrionScripting in ConfigEcfAccess.cs 
        // https://github.com/GitHub-TC/EmpyrionScripting/blob/a4f6073e812f38ab5ad90534bf7e2402ac15920d/EmpyrionScripting/ConfigEcfAccess.cs#L16
        //
        // public IReadOnlyDictionary<string, int> BlockIdMapping { get; set; }

        var invertedMap = map.ToDictionary(x => x.Value, x => x.Key).OrderBy(kvp => kvp.Value);

        // Write to file
        using StreamWriter writer = File.CreateText("NameIdMapping.json");

        var serializer = JsonSerializer.Create(new JsonSerializerSettings() { Formatting = Formatting.Indented });
        serializer.Serialize(writer, invertedMap);

        // Report
        sw.Stop();

        AnsiConsole.MarkupLine($"[green]Finished![/] Generated NameIdMapping.json in {sw.ElapsedMilliseconds:0.00}ms");

        // Write helpfull directions
        var modFolder = Path.Join(save.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender");
        AnsiConsole.WriteLine("To enable mapping in EmyprionBackpackExtender please do the following.");
        AnsiConsole.WriteLine($"1.) Place NameIdMapping.json in {modFolder}");
        AnsiConsole.WriteLine("2.) Open Configuration.json (in the same folder) with a text editor of your choice.");
        AnsiConsole.MarkupLine("3.) Set \"NameIdMappingFile\" equal to the [red]full[/] path of NameIdMapping.json");

        return 0;
    }
}