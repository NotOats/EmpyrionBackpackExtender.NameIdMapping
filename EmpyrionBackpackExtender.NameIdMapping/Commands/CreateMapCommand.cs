using EmpyrionBackpackExtender.NameIdMapping.GameFiles;
using Spectre.Console.Cli;
using Spectre.Console;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Newtonsoft.Json;
using System.ComponentModel;
using EmpyrionBackpackExtender.NameIdMapping.BackpackExtender;

namespace EmpyrionBackpackExtender.NameIdMapping.Commands;

internal class CreateMappingSettings : GameSettings
{
    [CommandOption("--save-local")]
    [DefaultValue(true)]
    [Description("Save a local copy of NameIdMapping.json")]
    public bool SaveLocal { get; set; }

    [CommandOption("--save-server")]
    [Description("Save the name id map to EmpyrionBackpackExtender's config file.")]
    public bool? SaveServer { get; set; }

    [CommandOption("--force-config-update")]
    [DefaultValue(false)]
    [Description("Skips user confirmation to update EmpyrionBackpackExtender's config file.")]
    public bool ForceConfigUpdate { get; set; }
}

internal class CreateMapCommand : AsyncCommand<CreateMappingSettings>
{
    private readonly IFileSystem _fileSystem;

    public CreateMapCommand(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public override Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] CreateMappingSettings settings)
    {
        settings.PromptForMissing(_fileSystem);

        // Load game files & generate id/name map
        var save = new SaveGame(_fileSystem, settings.ServerFolder!, settings.ServerConfig!);
        DisplaySaveGameInfo(save);

        var map = CreateNameIdMap(save, settings.EcfFiles!.Split(','));
        AnsiConsole.WriteLine($"Generated Real Id <-> Name map with {map.Count} entries");

        // Save a local copy
        if (settings.SaveLocal)
        {
            WriteNameIdMapFile(map, "NameIdMapping.json");
            AnsiConsole.WriteLine("NameIdMapping.json saved to current directory.");
        }

        // Check if file should be installed to the server
        if (settings.SaveServer.HasValue 
            && !settings.SaveServer.Value)
            return Task.FromResult(0);

        if(!settings.SaveServer.HasValue 
            && !AnsiConsole.Confirm("Install mapping file in server's EmpyrionBackpackExtender configuration?"))
            return Task.FromResult(0);

        // Save & Configure server copy
        var serverMapFile = _fileSystem.Path.Join(save.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender\NameIdMapping.json");
        WriteNameIdMapFile(map, serverMapFile);

        BackpackConfig config;
        try
        {
            config = new BackpackConfig(_fileSystem, save);
        }
        catch(FileNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]ERROR[/]: Config file does not exist at {ex.FileName}");
            return Task.FromResult(2); // ERROR_FILE_NOT_FOUND
        }

        DisplayBackpackConfigInfo(config, serverMapFile);

        if (!settings.ForceConfigUpdate
            && config.NameIdMappingFile != config.NameIdMappingFileDefault
            && !AnsiConsole.Confirm("Overwrite existing entry?"))
            return Task.FromResult(0);

        config.NameIdMappingFile = serverMapFile;
        config.Save();

        AnsiConsole.WriteLine("EmpyrionBackpackExtender configuration updated.");

        return Task.FromResult(0);
    }

    private void DisplaySaveGameInfo(SaveGame save)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow(new Text("Root Folder"), new TextPath(save.ServerDirectory));
        grid.AddRow(new Text("Game Name"), new Text(save.ServerConfig.GameName));
        grid.AddRow(new Text("Scenario"), new Text(save.ServerConfig.CustomScenario));

        AnsiConsole.Write(new Panel(grid).Header("Server Information").Expand());
    }

    private void DisplayBackpackConfigInfo(BackpackConfig config, string newMapFile)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow(new Text("Config File"), new TextPath(config.ConfigFile));
        grid.AddRow(new Text("Current NameIdMappingFile"),
            config.NameIdMappingFile == config.NameIdMappingFileDefault
            ? new Text(config.NameIdMappingFile) : new TextPath(config.NameIdMappingFile));
        grid.AddRow(new Text("New NameIdMappingFile"), new TextPath(newMapFile));

        AnsiConsole.Write(new Panel(grid).Header("Backpack Configuration").Expand());
    }

    private IReadOnlyDictionary<string, int> CreateNameIdMap([NotNull] SaveGame save, [NotNull] IEnumerable<string> ecfFiles)
    {
        var map = save.CreateRealIdToNameMap(ecfFiles);

        // Change to the correct dictionary found in GitHub-TC/EmpyrionScripting in ConfigEcfAccess.cs 
        // https://github.com/GitHub-TC/EmpyrionScripting/blob/a4f6073e812f38ab5ad90534bf7e2402ac15920d/EmpyrionScripting/ConfigEcfAccess.cs#L16
        // public IReadOnlyDictionary<string, int> BlockIdMapping { get; set; }

        return map.OrderBy(kvp => kvp.Value).ToDictionary(x => x.Value, x => x.Key);
    }

    private void WriteNameIdMapFile([NotNull] IReadOnlyDictionary<string, int> map, string file)
    {
        using StreamWriter writer = _fileSystem.File.CreateText(file);

        var serializer = JsonSerializer.Create(new JsonSerializerSettings() { Formatting = Formatting.Indented });
        serializer.Serialize(writer, map);
    }
}