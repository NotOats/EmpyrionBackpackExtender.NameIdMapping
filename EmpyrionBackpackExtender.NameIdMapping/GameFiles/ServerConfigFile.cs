using System.Runtime.CompilerServices;
using YamlDotNet.Serialization;

[assembly: InternalsVisibleTo("EmpyrionBackpackExtender.NameIdMapping.Tests")]
namespace EmpyrionBackpackExtender.NameIdMapping.GameFiles;

/// <summary>
/// Represents an immutable server config file, typically named Dedicated.yaml
/// </summary>
internal class ServerConfigFile
{
    // ServerConfig
    /// <summary>
    /// The server's name
    /// </summary>
    public string ServerName { get; }

    /// <summary>
    /// The save game directory, typically "<SERVER_ROOT>\Saves\"
    /// </summary>
    public string SaveDirectory { get; }

    /// <summary>
    /// The AdminConfig file, typically "<SaveDirectory>\adminconfig.yaml"
    /// </summary>
    public string AdminConfigFile { get; }


    // GameConfig
    /// <summary>
    /// The name of the game's save directory, located at "<SaveDirectory>\Games\<GameName>\"
    /// </summary>
    public string GameName { get; }

    /// <summary>
    /// Name of the scenario the server runs.
    /// Used to locate the scenario folder at "<SERVER_ROOT>\Content\Scenarios\<CustomScenario>\"
    /// </summary>
    public string CustomScenario { get; }

    private ServerConfigFile(
        string serverName,
        string saveDirectory,
        string adminConfigFile,
        string gameName,
        string customScenario)
    {
        ServerName = serverName;
        SaveDirectory = saveDirectory ?? "Saves";
        AdminConfigFile = adminConfigFile ?? "adminconfig.yaml";

        GameName = gameName;
        CustomScenario = customScenario;
    }

    /// <summary>
    /// Reads a server's dedicated config file, typically located at "<SERVER_ROOT>\dedicated.yaml"
    /// </summary>
    /// <param name="file">The config file to load</param>
    /// <returns></returns>
    public static ServerConfigFile Load(string file)
    {
        if (!File.Exists(file))
            return null;

        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();

        using (var reader = new StreamReader(file))
        {
            var builder = deserializer.Deserialize<ServerConfigFileBuilder>(reader);

            return builder.Build();
        }
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private class ServerConfigFileBuilder
    {
        public ServerConfigSection ServerConfig { get; set; }
        public GameConfigSection GameConfig { get; set; }

        public ServerConfigFile Build()
        {
            return new ServerConfigFile(
                ServerConfig.ServerName,
                ServerConfig.SaveDirectory,
                ServerConfig.AdminConfigFile,
                GameConfig.GameName,
                GameConfig.CustomScenario);
        }

        public class ServerConfigSection
        {
            [YamlMember(Alias = "Srv_Name")]
            public string ServerName { get; set; }
            public string SaveDirectory { get; set; }
            public string AdminConfigFile { get; set; }
        }

        public class GameConfigSection
        {
            public string GameName { get; set; }
            public string CustomScenario { get; set; }
        }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
