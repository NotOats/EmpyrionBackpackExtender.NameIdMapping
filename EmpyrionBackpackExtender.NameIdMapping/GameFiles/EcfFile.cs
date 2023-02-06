using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

[assembly: InternalsVisibleTo("EmpyrionBackpackExtender.NameIdMapping.Tests")]
namespace EmpyrionBackpackExtender.NameIdMapping.GameFiles;

/// <summary>
/// A simply processed EGS ecf file, currently only supports Type, Id, and Name attributes
/// </summary>
internal class EcfFile
{
    private readonly IFileSystem _fileSystem;
    private readonly string _file;
    private readonly List<EcfEntry> _entries;

    /// <summary>
    /// The extracted entries
    /// </summary>
    public IList<EcfEntry> Entries => _entries;

    /// <summary>
    /// The ecf File name
    /// </summary>
    public string FileName => _fileSystem.Path.GetFileNameWithoutExtension(_file);

    /// <summary>
    /// Creates a new EcfFile for the specifed file
    /// </summary>
    /// <param name="file">The ecf file</param>
    public EcfFile(IFileSystem fileSystem, string file)
    {
        _fileSystem = fileSystem;
        _file = file;

        _entries = ParseFile().ToList();
    }

    /// <summary>
    /// Creates a RealId, Name map for the given ecf files.
    /// </summary>
    /// <param name="files">The ecf files</param>
    /// <returns></returns>
    public static IReadOnlyDictionary<int, string> CreateRealIdNameMap(IFileSystem fileSystem, IEnumerable<string> files)
    {
        var result = new ConcurrentDictionary<int, string>();

        foreach (var file in files)
        {
            var parser = new EcfFile(fileSystem, file);

            Parallel.ForEach(parser.Entries, entry =>
            {
                result.AddOrUpdate(entry.RealId, entry.Name, (k, o) => entry.Name);
            });
        }

        return result;
    }

    private IEnumerable<EcfEntry> ParseFile()
    {
        var contents = _fileSystem.File.ReadAllText(_file);

        // Replace multiline comments
        contents = Regex.Replace(contents, "/\\*[^*]*\\*+(?:[^/*][^*]*\\*+)*/", "");

        // Replace single line comments, replace with newline to fix end of line parsing
        contents = Regex.Replace(contents, "#(?:.*?)\r?\n", Environment.NewLine);

        // Split & Process lines
        return contents.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            .AsParallel()
            .Select(ProcessLine)
            .Where(entry => entry != null)
            .Select(entry => entry!);
    }

    private static EcfEntry? ProcessLine(string line)
    {
        if (string.IsNullOrEmpty(line))
            return null;

        var match = Regex.Match(line, @"{ \+?(Item|Block) Id: ([0-9]+), Name: ([^\,\r\n]+)");
        if (match.Success)
        {
            var type = match.Groups[1].Value == "Item" ? EcfEntryType.Item : EcfEntryType.Block;
            var id = int.Parse(match.Groups[2].Value);
            var name = match.Groups[3].Value.Trim();

            return new EcfEntry(type, id, name);
        }

        return null;
    }
}

public enum EcfEntryType
{
    Item,
    Block
}

/// <summary>
/// ECF file entry
/// </summary>
/// <param name="Type">The entry type, ex: Item, Block</param>
/// <param name="Id">The entry Id, this is specific to type</param>
/// <param name="Name">The entry name, this is typically a unique key for the specified item/block</param>
public record class EcfEntry(EcfEntryType Type, int Id, string Name)
{
    /// <summary>
    /// Calculates the "real" in-game Id for the entry based on it's type.
    /// </summary>
    public int RealId
    {
        get
        {
            switch (Type)
            {
                case EcfEntryType.Item:
                    return Id + 4096; // Offset to handle blocks
                default:
                    return Id;
            }
        }
    }
}
