using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("EmpyrionBackpackExtender.NameIdMapping.Tests")]
namespace EmpyrionBackpackExtender.NameIdMapping.GameFiles;

internal class SaveGame
{
    private readonly IFileSystem _fileSystem;

    //
    // Game directories
    //
    /// <summary>
    /// The server's root directory
    /// </summary>
    public string ServerDirectory { get; }

    /// <summary>
    /// The server's content directory, typically "<ServerDirectory>\Content"
    /// </summary>
    public string ContentDirectory => _fileSystem.Path.Combine(ServerDirectory, "Content");

    /// <summary>
    /// The server's save game directory, typically "<ServerDirectory>\Saves\Games\<GameName>"
    /// </summary>
    public string SaveGameDirectory => _fileSystem.Path.Combine(ServerDirectory, ServerConfig.SaveDirectory, "Games", ServerConfig.GameName);

    /// <summary>
    /// The server's scenario content directory, typically "<ServerDirectory>\Content\Scenarios\<CustomScenario>\Content"
    /// </summary>
    public string? ScenarioContentDirectory => !string.IsNullOrWhiteSpace(ServerConfig.CustomScenario)
            ? _fileSystem.Path.Combine(ContentDirectory, "Scenarios", ServerConfig.CustomScenario, "Content") : null;

    //
    // Game configuration info
    //
    /// <summary>
    /// Contains information about the server's configuration file typically named "dedicated.yaml"
    /// </summary>
    public ServerConfigFile ServerConfig { get; }


    public SaveGame(IFileSystem fileSystem, string serverDirectory, string dedicatedConfigFileName)
    {
        _fileSystem = fileSystem;
        ServerDirectory = serverDirectory;

        // Load server config
        var configFile = _fileSystem.Path.Combine(ServerDirectory, dedicatedConfigFileName);
        if (!_fileSystem.File.Exists(configFile))
            throw new FileNotFoundException("Failed to load server config file", configFile);

        ServerConfig = ServerConfigFile.Load(_fileSystem, configFile);
    }

    public IReadOnlyDictionary<int, string> CreateRealIdToNameMap(IEnumerable<string> ecfItemAndBlockFiles)
    {
        var ecfFiles = ReadEcfFiles(ecfItemAndBlockFiles);
        return ItemMapFromEcfFiles(ecfFiles)
            .Union(ReadBlockMapping())
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public IReadOnlyList<string> ScenarioEcfFiles()
    {
        var contentDirectory = ScenarioContentDirectory ?? ContentDirectory;
        var ecfDirectory = _fileSystem.Path.Combine(contentDirectory, "Configuration");

        return _fileSystem.Directory.GetFiles(ecfDirectory, "*.ecf");
    }

    private IEnumerable<EcfFile> ReadEcfFiles(IEnumerable<string> files)
    {
        string FindFullPath(string file)
        {
            if (ScenarioContentDirectory != null)
                return _fileSystem.Path.Combine(ScenarioContentDirectory, "Configuration", file);

            return _fileSystem.Path.Combine(ContentDirectory, "Configuration", file);
        }

        foreach(var file in files.Select(FindFullPath))
        {
            if (!_fileSystem.File.Exists(file))
                continue;

            yield return new EcfFile(_fileSystem, file);
        }
    }

    /// <summary>
    /// Returns a Real Id (offset calculated) of blocks & items from the specified ecf files.
    /// </summary>
    /// <param name="files"></param>
    /// <returns></returns>
    private static IDictionary<int, string> ItemMapFromEcfFiles(IEnumerable<EcfFile> files)
    {
        var result = new Dictionary<int, string>();

        foreach (var file in files)
        {
            foreach (var entry in file.Entries)
            {
                result[entry.RealId] = entry.Name;
            }
        }

        return result;
    }

    private IEnumerable<KeyValuePair<int, string>> ReadBlockMapping()
    {
        var file = _fileSystem.Path.Combine(SaveGameDirectory, "blocksmap.dat");

        if (!_fileSystem.File.Exists(file))
            yield break;

        using var stream = _fileSystem.File.Open(file, FileMode.Open);
        using var reader = new BinaryReader(stream);
        reader.BaseStream.Seek(9, SeekOrigin.Begin);

        while (reader.BaseStream.Position != reader.BaseStream.Length)
        {
            var len = reader.ReadByte();
            var name = Encoding.ASCII.GetString(reader.ReadBytes(len));

            var id = reader.ReadByte() | reader.ReadByte() << 8;

            yield return new KeyValuePair<int, string>(id, name);
        }
    }
}
