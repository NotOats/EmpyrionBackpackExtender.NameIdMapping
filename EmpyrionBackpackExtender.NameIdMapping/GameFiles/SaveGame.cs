using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmpyrionBackpackExtender.NameIdMapping.GameFiles
{
    public class SaveGame
    {
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
        public string ContentDirectory { get; }

        /// <summary>
        /// The server's save game directory, typically "<ServerDirectory>\Saves\Games\<GameName>"
        /// </summary>
        public string SaveGameDirectory { get; }

        /// <summary>
        /// The server's scenario content directory, typically "<ServerDirectory>\Content\Scenarios\<CustomScenario>\Content"
        /// </summary>
        public string? ScenarioContentDirectory { get; }

        //
        // Game configuration info
        //
        /// <summary>
        /// Contains information about the server's configuration file typically named "dedicated.yaml"
        /// </summary>
        public ServerConfigFile ServerConfig { get; }


        public SaveGame(string serverDirectory, string dedicatedConfigFileName)
        {
            ServerDirectory = serverDirectory;

            // Load server config
            var configFile = Path.Combine(ServerDirectory, dedicatedConfigFileName);
            if (!File.Exists(configFile))
                throw new FileNotFoundException("Failed to load server config file", configFile);

            ServerConfig = ServerConfigFile.Load(configFile);

            // Calculate folder paths
            ContentDirectory = Path.Combine(ServerDirectory, "Content");
            SaveGameDirectory = Path.Combine(ServerDirectory, ServerConfig.SaveDirectory, "Games", ServerConfig.GameName);
            ScenarioContentDirectory = string.IsNullOrWhiteSpace(ServerConfig.CustomScenario)
                ? null : Path.Combine(ContentDirectory, "Scenarios", ServerConfig.CustomScenario, "Content");
        }

        public IReadOnlyDictionary<int, string> CreateRealIdToNameMap(IEnumerable<string> ecfItemAndBlockFiles)
        {
            var ecfFiles = ecfItemAndBlockFiles.Select(file => ReadEcfFile(file)).ToList();
            return ItemMapFromEcfFiles(ecfFiles)
                .Union(ReadBlockMapping())
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public IReadOnlyList<string> ScenarioEcfFiles()
        {
            var contentDirectory = ScenarioContentDirectory ?? ContentDirectory;
            var ecfDirectory = Path.Combine(contentDirectory, "Configuration");

            return Directory.GetFiles(ecfDirectory, "*.ecf");
        }

        /// <summary>
        /// Reads an ecf file, trying the active scenario first or defaulting to the stock file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private EcfFile ReadEcfFile(string fileName)
        {
            if (ScenarioContentDirectory != null)
            {
                var scenarioFile = Path.Combine(ScenarioContentDirectory, "Configuration", fileName);

                if (File.Exists(scenarioFile))
                {
                    return new EcfFile(scenarioFile);
                }
            }

            var stockFile = Path.Combine(ContentDirectory, "Configuration", fileName);
            return new EcfFile(stockFile);
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
            var file = Path.Combine(SaveGameDirectory, "blocksmap.dat");

            if (!File.Exists(file))
                yield break;

            using var stream = File.Open(file, FileMode.Open);
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
}
