using Newtonsoft.Json;
using System.IO.Abstractions;

namespace EmpyrionBackpackExtender.NameIdMapping.BackpackExtender;

// Loading & Saving is copied from EmpyrionNetAPIAccess to maintain compatibility
// https://github.com/GitHub-TC/EmpyrionNetAPIAccess/blob/master/EmpyrionNetAPITools/ConfigurationManager.cs
internal class Backpack
{
    private readonly IFileSystem _fileSystem;
    private readonly Lazy<BackpackData> _backpackData;

    public string File { get; }
    public BackpackType BackpackType { get; }
    public BackpackData BackpackData => _backpackData.Value;

    public Backpack(IFileSystem fileSystem, string file, BackpackType type)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        File = file ?? throw new ArgumentNullException(nameof(file));
        BackpackType = type;

        if (!_fileSystem.File.Exists(File))
            throw new FileNotFoundException("Backpack not found", File);

        _backpackData = new Lazy<BackpackData>(ReadBackpackData);
    }

    /// <summary>
    /// Converts all items to use names instead of ids. This will only change items currently missing a name.
    /// </summary>
    /// <param name="realIdNameMap">The real id <-> name map to use</param>
    /// <returns>The number of items updated</returns>
    /// <exception cref="ArgumentNullException">Thrown when readlIdNameMap is null</exception>
    /// <exception cref="ArgumentException">Throw when realIdNameMap is empty</exception>
    /// <exception cref="KeyNotFoundException">Thrown when an item id can't be matched to a name</exception>
    public int ConvertRealIdsToName(IReadOnlyDictionary<int, string> realIdNameMap)
    {
        if (realIdNameMap == null) throw new ArgumentNullException(nameof(realIdNameMap));
        if (realIdNameMap.Count <= 0) throw new ArgumentException("empty dictionary is not allowed", nameof(realIdNameMap));

        var count = 0;
        var dirty = BackpackData.Backpacks
            .SelectMany(backpack => backpack.Items)
            .Where(item => string.IsNullOrEmpty(item.name));

        foreach(var item in dirty)
        {
            if (!realIdNameMap.TryGetValue(item.id, out var name))
                throw new KeyNotFoundException($"item id does not exist in map");

            if(item.name != name)
            {
                item.name = name;
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Converts the id entry on all items to the value specified in realIdNameMap.
    /// Note: This method requires all items to have an item name
    /// </summary>
    /// <param name="realIdNameMap">The real id <-> name map to use</param>
    /// <returns>The number of items updated</returns>
    /// <exception cref="ArgumentNullException">Thrown when readlIdNameMap is null</exception>
    /// <exception cref="ArgumentException">Throw when realIdNameMap is empty</exception>
    public int ConvertNameToRealIds(IReadOnlyDictionary<int, string> realIdNameMap)
    {
        if (realIdNameMap == null) throw new ArgumentNullException(nameof(realIdNameMap));
        if (realIdNameMap.Count <= 0) throw new ArgumentException("empty dictionary is not allowed", nameof(realIdNameMap));

        // Reverse dictionary for name to id look up
        var map = new Dictionary<string, int>();
        foreach(var entry in realIdNameMap)
        {
            if(!map.ContainsKey(entry.Value))
                map.Add(entry.Value, entry.Key);
        }

        return ConvertNameToRealIds(map);
    }

    /// <summary>
    /// Converts the id entry on all items to the value specified in nameRealIdMap.
    /// Note: This method requires all items to have an item name
    /// </summary>
    /// <param name="nameRealIdMap">The real id <-> name map to use</param>
    /// <returns>The number of items updated</returns>
    /// <exception cref="ArgumentNullException">Thrown when nameRealIdMap is null</exception>
    /// <exception cref="ArgumentException">Throw when nameRealIdMap is empty</exception>
    /// <exception cref="KeyNotFoundException">Thrown when an item name is missing or it can't be matched to an id</exception>
    public int ConvertNameToRealIds(IReadOnlyDictionary<string, int> nameRealIdMap)
    {
        if (nameRealIdMap == null) throw new ArgumentNullException(nameof(nameRealIdMap));
        if (nameRealIdMap.Count <= 0) throw new ArgumentException("empty dictionary is not allowed", nameof(nameRealIdMap));

        // Do the conversion
        var count = 0;
        var items = BackpackData.Backpacks
            .SelectMany(backpack => backpack.Items);

        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.name))
                throw new KeyNotFoundException("item is missing name");

            if (!nameRealIdMap.TryGetValue(item.name, out var id))
                throw new KeyNotFoundException("item name dfoes not exist in map");

            if (item.id != id)
            {
                item.id = id;
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Updates item names in all backpacks.
    /// </summary>
    /// <param name="oldName">The old item name</param>
    /// <param name="newName">The new item name</param>
    /// <returns>The number of items updated</returns>
    /// <exception cref="ArgumentException">Thrown when oldName or newName is null or empty.</exception>
    public int UpdateItemName(string oldName, string newName)
    {
        if (string.IsNullOrEmpty(oldName)) throw new ArgumentException("name is not allowed to be null or empty", nameof(oldName));
        if (string.IsNullOrEmpty(newName)) throw new ArgumentException("name is not allowed to be null or empty", nameof(newName));

        var count = 0;
        var dirty = BackpackData.Backpacks
            .SelectMany(backpack => backpack.Items)
            .Where(item => item.name == oldName);

        foreach(var item in dirty)
        {
            item.name = newName;
            count++;
        }

        return count;
    }

    public void Save()
    {
        var data = JsonConvert.SerializeObject(BackpackData, Formatting.Indented);

        _fileSystem.File.WriteAllText(File, data);
    }

    private BackpackData ReadBackpackData()
    {
        var serializer = new JsonSerializer();
        using var contents = _fileSystem.File.OpenText(File);

        var backpack = serializer.Deserialize(contents, typeof(BackpackData));
        if (backpack == null)
            throw new JsonSerializationException("Failed to read BackpackData");

        return (BackpackData)backpack;
    }
}