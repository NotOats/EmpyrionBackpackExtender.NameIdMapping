using EmpyrionBackpackExtender.NameIdMapping.BackpackExtender;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace EmpyrionBackpackExtender.NameIdMapping.Commands;

internal class ConvertToNameFolderSettings : CommandSettings
{
    [CommandOption("--item-list <ItemList_file>")]
    [Description("The ItemList.csv to use, this is exported from the EPF mod.")]
    public string? ItemList { get; set; }

    [CommandOption("--backpack-folder <backpack_folder>")]
    [Description("The folder containing backpack json files to convert to name format.")]
    public string? BackpackFolder { get; set; }

    public void PromptForMissing(IAnsiConsole console)
    {
        ItemList ??= console.Ask<string>("Please enter ItemList.csv path: ");
        BackpackFolder ??= console.Ask<string>("Please enter the backpack folder: ");
    }
}

internal class ConvertToNameFolderCommand : AsyncCommand<ConvertToNameFolderSettings>
{
    private readonly IAnsiConsole _console;
    private readonly IFileSystem _fileSystem;

    public ConvertToNameFolderCommand(IAnsiConsole console, IFileSystem fileSystem)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ConvertToNameFolderSettings settings)
    {
        settings.PromptForMissing(_console);

        var map = await ParseItemListFile(settings.ItemList!);
        _console.WriteLine($"Generated Real Id <-> Name map with {map.Count} entries");

        var backpacks = _fileSystem.Directory.GetFiles(settings.BackpackFolder!, "*.json")
            .Select(file => new Backpack(_fileSystem, file, BackpackType.Unknown));

        await ProcessBackpacks(map, backpacks);

        return 0;
    }

    private async Task<IReadOnlyDictionary<int, string>> ParseItemListFile(string itemListFile)
    {
        if (!_fileSystem.File.Exists(itemListFile))
            throw new FileNotFoundException("ItemList file not found", itemListFile);

        var lines = await _fileSystem.File.ReadAllLinesAsync(itemListFile);

        return lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(';'))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => int.Parse(parts[1]), parts => parts[0]);
    }

    private async Task ProcessBackpacks(IReadOnlyDictionary<int, string> map, IEnumerable<Backpack> backpacks)
    {
        // Update stats
        var totalBackpacks = 0;
        var backpacksUpdated = 0;
        var itemsUpdated = 0;
        var updateErrors = 0;
        var saveErrors = 0;

        // TPL DataFlow blocks
        var convertBackpacks = new TransformBlock<Backpack, Backpack?>(
            bp =>
            {
                var changes = 0;

                try
                {
                    changes = bp.ConvertRealIdsToName(map);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref updateErrors);

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
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });

        var saveBackpacks = new ActionBlock<Backpack?>(
            bp =>
            {
                try
                {
                    bp?.Save();
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref saveErrors);

                    _console.MarkupLine($"[red]ERROR[/]: Failed to save backpack at {bp?.File}");
                    _console.WriteException(ex);
                }
            },
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });

        convertBackpacks.LinkTo(saveBackpacks,
            new DataflowLinkOptions { PropagateCompletion = true },
            bp => bp != null);
        convertBackpacks.LinkTo(DataflowBlock.NullTarget<Backpack?>());

        // Start Update
        var sw = Stopwatch.StartNew();

        foreach(var backpack in backpacks)
        {
            totalBackpacks++;
            await convertBackpacks.SendAsync(backpack);
        }

        convertBackpacks.Complete();
        await saveBackpacks.Completion;

        sw.Stop();

        // Report
        _console.WriteLine($"Finished processing {totalBackpacks} player backpacks in {sw.Elapsed.TotalSeconds:n}s.");
        _console.WriteLine($"{backpacksUpdated} Player backpacks updated");
        _console.WriteLine($"{itemsUpdated} Items updated");

        if (updateErrors != 0)
            _console.WriteLine($"[red]ERROR[/]: {updateErrors} Update errors");

        if (saveErrors != 0)
            _console.WriteLine($"[red]ERROR[/]: {saveErrors} Save errors");
    }
}
