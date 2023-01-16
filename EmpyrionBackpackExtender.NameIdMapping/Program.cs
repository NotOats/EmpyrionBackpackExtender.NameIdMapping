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
    [CommandOption("--folder")]
    [Description("The server's root folder (where 'BuildNumber.txt' is located)")]
    public string? ServerFolder { get; set; }

    [CommandOption("--config")]
    [DefaultValue("dedicated.yaml")]
    [Description("The server's configuration file.")]
    public string? ServerConfig { get; set; }


    [CommandOption("--ecf")]
    [Description("A comma seperated string of ecf files to read Item & Blocks from")]
    public string? EcfFiles { get; set; }

    public void PromptForMissing()
    {
        ServerFolder ??= AnsiConsole.Ask<string>("What is your server's [green]root folder[/] (where 'BuildNumber.txt' is located)?");

        // Read or Confirm defaults are good
        if (ServerConfig == null || ServerConfig == "dedicated.yaml")
        {
            ServerConfig = AnsiConsole.Ask<string>("What is your server's [green]configuration file[/]?", "dedicated.yaml");
        }

        // Pull available list for selection
        if (EcfFiles == null)
        {
            var save = new SaveGame(ServerFolder, ServerConfig);
            string[] options = save.ScenarioEcfFiles().Select(Path.GetFileName).Where(file => file != null).ToArray()!;
            var defaults = new[] { "BlocksConfig.ecf", "ItemsConfig.ecf" }.Where(options.Contains);

            var files = AnsiConsole.Prompt(
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
        var ecfs = settings.EcfFiles!.Split(',');
        var map = save.CreateRealIdToNameMap(ecfs);
        AnsiConsole.WriteLine($"Generated Real Id <-> Name map with {map.Count} entries");

        // Change to the correct dictionary found in GitHub-TC/EmpyrionScripting in ConfigEcfAccess.cs 
        // https://github.com/GitHub-TC/EmpyrionScripting/blob/a4f6073e812f38ab5ad90534bf7e2402ac15920d/EmpyrionScripting/ConfigEcfAccess.cs#L16
        //
        // public IReadOnlyDictionary<string, int> BlockIdMapping { get; set; }

        var invertedMap = map.OrderBy(kvp => kvp.Value).ToDictionary(x => x.Value, x => x.Key) as IReadOnlyDictionary<string, int>;

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