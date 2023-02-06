using System.Collections.Concurrent;
using System.Reflection;


namespace EmpyrionBackpackExtender.NameIdMapping.Tests;

internal static class MockData
{
    // File & Folder properties

    public static string GameFilesDirectory { get; }
    public static string ContentDirectory => Path.Combine(GameFilesDirectory, "Content");
    public static string SaveDirectory => Path.Combine(GameFilesDirectory, SaveDirectoryName);
    public static string SaveGameDirectory => Path.Combine(SaveDirectory, "Games", GameName);
    public static string ScenarioContentDirectory => Path.Combine(ContentDirectory, "Scenarios", CustomScenario, "Content");

    // Config information
    public static string ServerName => "Test Server";
    public static string SaveDirectoryName => "Saves";
    public static string ServerConfigFile => "dedicated.yaml";
    public static string AdminConfigFile => "adminconfig.yaml";
    public static string GameName => "DediGame";
    public static string CustomScenario => "Reforged Eden 1.9";
    public static IReadOnlyList<string> ItemAndBlockFiles => new[] { "Config_RE.ecf", "BlocksConfig.ecf", "ItemsConfig.ecf" };


    static MockData()
    {
        var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().Location);
        var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);

        var projectDirectory = Path.GetDirectoryName(codeBasePath);
        if (projectDirectory == null)
            throw new DirectoryNotFoundException("Failed to find Project Directory");

        GameFilesDirectory = Path.Combine(projectDirectory, "MockGameData");
        if(!Directory.Exists(GameFilesDirectory))
            throw new DirectoryNotFoundException($"GameFilesDirectory not found at '{GameFilesDirectory}'");
    }

    public static IReadOnlyDictionary<string, byte[]> ReadMockGameFiles()
    {
        var files = Directory.GetFiles(GameFilesDirectory, "*.*", SearchOption.AllDirectories);

        return files.AsParallel().ToDictionary(file => file, File.ReadAllBytes);

        /*
        var result = new ConcurrentDictionary<string, byte[]>();

        Parallel.ForEach(files, file =>
        {
            var data = File.ReadAllBytes(file);

            result.TryAdd(file, data);
        });

        return result;
        */
    }
}
