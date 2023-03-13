using EmpyrionBackpackExtender.NameIdMapping.GameFiles;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmpyrionBackpackExtender.NameIdMapping.BackpackExtender;

internal enum BackpackType
{
    Player,
    Unknown
}

internal class BackpackRepository
{
    private readonly IFileSystem _fileSystem;
    private readonly BackpackConfig _config;

    public BackpackType BackpackType { get; }

    public BackpackRepository(IFileSystem fileSystem, BackpackConfig config, BackpackType type)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        BackpackType = type;
    }

    public BackpackRepository(IFileSystem fileSystem, SaveGame save, BackpackType type)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        if(save == null) throw new ArgumentNullException(nameof(save));

        _config = new BackpackConfig(_fileSystem, save);

        BackpackType = type;
    }

    public IEnumerable<Backpack> ReadBackpacks()
    {
        var folder = SelectBackpackFolder();
        if (folder == null)
            return Enumerable.Empty<Backpack>();

        return _fileSystem.Directory.GetFiles(folder, "*.json")
            .Select(file => new Backpack(_fileSystem, file, BackpackType));
    }

    private string? SelectBackpackFolder()
    {
        var root = _fileSystem.Path.GetDirectoryName(_config.ConfigFile);
        if (root == null)
            return null;

        string pattern = BackpackType switch
        {
            BackpackType.Player => _fileSystem.Path.Combine(root, _config.PersonalBackpackPattern),
            _ => throw new NotSupportedException($"{BackpackType} is not supported"),
        };

        return _fileSystem.Path.GetDirectoryName(pattern);
    }
}
