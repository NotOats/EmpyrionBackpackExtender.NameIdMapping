using EmpyrionBackpackExtender.NameIdMapping;
using EmpyrionBackpackExtender.NameIdMapping.GameFiles;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

AnsiConsole.Write(new FigletText("NameIdMapping Generator").Centered());

var app = new CommandApp<DumpNameIdMappingFile>();
return app.Run(args);

class GameSettings : CommandSettings
{
    [CommandOption("--folder")]
    [Description("The server's root folder (where 'BuildNumber.txt' is located)")]
    public string? ServerFolder { get; set; }

    [CommandOption("--config")]
    [Description("The server's configuration file.")]
    public string? ServerConfig { get; set; }


    [CommandOption("--ecf")]
    [Description("A comma seperated string of ecf files to read Item & Blocks from")]
    public string? EcfFiles { get; set; }

    [CommandOption("--save-local")]
    [DefaultValue(true)]
    [Description("Save a local copy of NameIdMapping.json")]
    public bool SaveLocal { get; set; }

    [CommandOption("--save-server")]
    [Description("Save the name id map to EmpyrionBackpackExtender's config file.")]
    public bool? SaveServer { get; set; }

    public void PromptForMissing()
    {
        ServerFolder ??= AnsiConsole.Ask<string>("What is your server's [green]root folder[/] (where 'BuildNumber.txt' is located)?");

        ServerConfig ??= AnsiConsole.Ask<string>("What is your server's [green]configuration file[/]?", "dedicated.yaml");

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

partial class DumpNameIdMappingFile : Command<GameSettings>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] GameSettings settings)
    {
        // Prompt for missing settings
        settings.PromptForMissing();

        // Load game files
        var save = new SaveGame(settings.ServerFolder!, settings.ServerConfig!);

        var uiPaths = new Grid();
        uiPaths.AddColumn();
        uiPaths.AddColumn();

        uiPaths.AddRow(new Text("Root Folder"), new TextPath(save.ServerDirectory));
        uiPaths.AddRow(new Text("Game Name"), new Text(save.ServerConfig.GameName));
        uiPaths.AddRow(new Text("Scenario"), new Text(save.ServerConfig.CustomScenario));

        AnsiConsole.Write(new Panel(uiPaths).Header("Server Information").Expand());

        // Generate the map
        var map = new GameNameIdMap(save, settings.EcfFiles!.Split(','));
        AnsiConsole.WriteLine($"Generated Real Id <-> Name map with {map.NameIdMap.Count} entries");

        // Save a local copy
        if(settings.SaveLocal)
        {
            map.SaveMap("NameIdMapping.json");
            AnsiConsole.WriteLine("NameIdMapping.json saved to current directory.");
        }

        // Check if file should be installed to the server
        if (settings.SaveServer.HasValue)
        {
            if (!settings.SaveServer.Value)
                return 0;
        }
        else
        {
            if (!AnsiConsole.Confirm("Install mapping file in server's EmpyrionBackpackExtender configuration?"))
                return 0;
        }

        // Where the map file will be installed on the server
        var mapFile = Path.Join(save.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender\NameIdMapping.json");

        // Read current configuration
        var configFile = Path.Join(save.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender\Configuration.json");
        if(!File.Exists(configFile))
        {
            AnsiConsole.MarkupLine($"[red]ERROR[/]: Config file does not exist at {configFile}");
            return 2; // ERROR_FILE_NOT_FOUND
        }

        var config = new BackpackConfig(configFile);

        // Dump config
        var uiConfig = new Grid();
        uiConfig.AddColumn();
        uiConfig.AddColumn();

        uiConfig.AddRow(new Text("Config File"), new TextPath(configFile));
        uiConfig.AddRow(new Text("Current NameIdMappingFile"),
            config.NameIdMappingFile == config.NameIdMappingFileDefault 
            ? new Text(config.NameIdMappingFile) : new TextPath(config.NameIdMappingFile));
        uiConfig.AddRow(new Text("New NameIdMappingFile"), new TextPath(mapFile));

        AnsiConsole.Write(new Panel(uiConfig).Header("Backpack Configuration").Expand());

        // Confirm overwrite
        if (config.NameIdMappingFile != config.NameIdMappingFileDefault 
            && !settings.SaveServer.HasValue
            && !AnsiConsole.Confirm("Overwrite existing entry?"))
                    return 0;

        // Write file & update config
        map.SaveMap(mapFile);
        config.Save(mapFile);

        AnsiConsole.WriteLine("EmpyrionBackpackExtender configuration updated.");

        return 0;
    }
}