using EmpyrionBackpackExtender.NameIdMapping.GameFiles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Abstractions;

namespace EmpyrionBackpackExtender.NameIdMapping.BackpackExtender;

internal class BackpackConfig
{
    private readonly IFileSystem _fileSystem;
    private readonly JObject _config;

    public string ConfigFile { get; }

    #region BackpackExtenderConfiguration Entries
    // From https://github.com/GitHub-TC/EmpyrionBackpackExtender/blob/master/EmpyrionBackpackExtender/BackpackExtenderConfiguration.cs
    public string NameIdMappingFile
    {
        get => _config["NameIdMappingFile"]?.ToString() ?? NameIdMappingFileDefault;
        set => _config["NameIdMappingFile"] = value;
    }

    public string NameIdMappingFileDefault => "filepath to the NameIdMapping.json e.g. from EmpyrionScripting for cross savegame support";

    public string PersonalBackpackPattern
    {
        get => _config["PersonalBackpack"]?["FilenamePattern"]?.ToString() ?? PersonalBackpackPatternDefault;
        //set => _config["PersonalBackpack"]["FilenamePattern"] = value;
    }

    public string PersonalBackpackPatternDefault => @"Personal\{0}.json";
    #endregion

    public BackpackConfig(IFileSystem fileSystem, SaveGame save)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        if (save == null) throw new ArgumentNullException(nameof(save));

        ConfigFile = fileSystem.Path.Join(save.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender\Configuration.json");

        _config = ReadFile();
    }

    public BackpackConfig(IFileSystem fileSystem, string configFile)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        ConfigFile = configFile ?? throw new ArgumentNullException(nameof(configFile));

        _config = ReadFile();
    }

    public void Save()
    {
        using var writer = _fileSystem.File.CreateText(ConfigFile);
        using var jsonWriter = new JsonTextWriter(writer);

        jsonWriter.Formatting = Formatting.Indented;

        _config.WriteTo(jsonWriter);
    }

    private JObject ReadFile()
    {
        if (!_fileSystem.File.Exists(ConfigFile))
            throw new FileNotFoundException($"{ConfigFile} does not exist");

        var contents = _fileSystem.File.ReadAllText(ConfigFile);
        return JObject.Parse(contents);
    }
}