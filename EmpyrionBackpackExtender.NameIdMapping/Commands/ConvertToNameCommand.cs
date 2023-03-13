using EmpyrionBackpackExtender.NameIdMapping.BackpackExtender;
using EmpyrionBackpackExtender.NameIdMapping.GameFiles;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace EmpyrionBackpackExtender.NameIdMapping.Commands;

internal class ConvertToNameSettings : GameSettings
{

}

internal class ConvertToNameCommand : AsyncCommand<ConvertToNameSettings>
{
    private readonly IAnsiConsole _console;
    private readonly IFileSystem _fileSystem;

    public ConvertToNameCommand(IAnsiConsole console, IFileSystem fileSystem)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ConvertToNameSettings settings)
    {
        settings.PromptForMissing(_console, _fileSystem);

        // Load game files & generate id/name map
        SaveGame save;
        try
        {
            save = new SaveGame(_fileSystem, settings.ServerFolder!, settings.ServerConfig!);
            DisplaySaveGameInfo(save);
        }
        catch (FileNotFoundException ex)
        {
            _console.MarkupLine($"[red]ERROR[/]: Config file does not exist at {ex.FileName}");
            return 2; // ERROR_FILE_NOT_FOUND
        }

        var map = save.CreateRealIdToNameMap(settings.EcfFiles!.Split(','));
        _console.WriteLine($"Generated Real Id <-> Name map with {map.Count} entries");

        // Report values
        var totalBackpacks = 0;
        var backpacksUpdated = 0;
        var itemsUpdated = 0;

        // TPL Dataflows
        var convertBackpacks = new TransformBlock<Backpack, Backpack?>(
            bp =>
            {
                var changes = 0;

                try
                {
                    changes = bp.ConvertRealIdsToName(map);
                }
                catch(Exception ex)
                {
                    _console.MarkupLine($"[red]ERROR[/]: Failed to convert backpack at {bp.File}");
                    _console.WriteException(ex);
                    return null;
                }

                if (changes == 0)
                    return null;

                Interlocked.Increment(ref backpacksUpdated);
                Interlocked.Add(ref itemsUpdated, changes);

                return bp;
            }, 
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
            });

        var saveBackpacks = new ActionBlock<Backpack?>(
            bp =>
            {
                try
                {
                    bp?.Save();
                }
                catch(Exception ex)
                {
                    _console.MarkupLine($"[red]ERROR[/]: Failed to save backpack at {bp?.File}");
                    _console.WriteException(ex);
                }
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
            });

        convertBackpacks.LinkTo(saveBackpacks, 
            new DataflowLinkOptions { PropagateCompletion = true }, 
            bp => bp != null);
        convertBackpacks.LinkTo(DataflowBlock.NullTarget<Backpack?>());

        // Load backpacks
        var bpRepository = new BackpackRepository(_fileSystem, save, BackpackType.Player);

        var sw = Stopwatch.StartNew();

        // Start & wait for completion
        foreach(var backpack in bpRepository.ReadBackpacks())
        {
            totalBackpacks++;
            await convertBackpacks.SendAsync(backpack);
        }

        convertBackpacks.Complete();
        await saveBackpacks.Completion;

        sw.Stop();

        _console.WriteLine($"Finished processing {totalBackpacks} player backpacks in {sw.Elapsed.TotalSeconds:n}s.");
        _console.WriteLine($"{backpacksUpdated} Player backpacks updated");
        _console.WriteLine($"{itemsUpdated} Items updated");

        return 0;
    }

    private void DisplaySaveGameInfo(SaveGame save)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow(new Text("Root Folder"), new TextPath(save.ServerDirectory));
        grid.AddRow(new Text("Game Name"), new Text(save.ServerConfig.GameName));
        grid.AddRow(new Text("Scenario"), new Text(save.ServerConfig.CustomScenario));

        _console.Write(new Panel(grid).Header("Server Information").Expand());
    }
}
