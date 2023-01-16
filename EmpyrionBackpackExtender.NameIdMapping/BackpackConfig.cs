using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EmpyrionBackpackExtender.NameIdMapping;

internal class BackpackConfig
{
    // From https://github.com/GitHub-TC/EmpyrionBackpackExtender/blob/master/EmpyrionBackpackExtender/BackpackExtenderConfiguration.cs
    private static readonly string _nameIdMapProperty = "NameIdMappingFile";
    private static readonly string _nameIdMapDefault = "filepath to the NameIdMapping.json e.g. from EmpyrionScripting for cross savegame support";

    public string ConfigFile { get; }

    public string NameIdMappingFile { get; }
    public string NameIdMappingFileDefault => _nameIdMapDefault;

    public BackpackConfig(string configFile)
    {
        ConfigFile = configFile;

        var obj = ReadFile();
        NameIdMappingFile = obj[_nameIdMapProperty]?.ToString() ?? _nameIdMapDefault;
    }

    public void Save(string nameIdMapProperty)
    {
        var obj = ReadFile();
        obj[_nameIdMapProperty] = nameIdMapProperty;

        SaveFile(obj);
    }

    private JObject ReadFile()
    {
        var contents = File.ReadAllText(ConfigFile);
        return JObject.Parse(contents);
    }

    private void SaveFile(JObject obj)
    {

        using var writer = File.CreateText(ConfigFile);
        using var jsonWriter = new JsonTextWriter(writer);

        jsonWriter.Formatting = Formatting.Indented;

        obj.WriteTo(jsonWriter);
    }
}