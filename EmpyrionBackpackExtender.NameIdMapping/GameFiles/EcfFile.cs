using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmpyrionBackpackExtender.NameIdMapping.GameFiles
{
    /// <summary>
    /// A simply processed EGS ecf file, currently only supports Type, Id, and Name attributes
    /// </summary>
    public class EcfFile
    {
        private readonly string _file;
        private readonly List<EcfEntry> _entries;

        /// <summary>
        /// The extracted entries
        /// </summary>
        public IList<EcfEntry> Entries => _entries;

        /// <summary>
        /// The ecf File name
        /// </summary>
        public string FileName => Path.GetFileNameWithoutExtension(_file);

        /// <summary>
        /// Creates a new EcfFile for the specifed file
        /// </summary>
        /// <param name="file">The ecf file</param>
        public EcfFile(string file)
        {
            _file = file;

            _entries = ParseFile().ToList();
        }

        /// <summary>
        /// Creates a RealId, Name map for the given ecf files.
        /// </summary>
        /// <param name="files">The ecf files</param>
        /// <returns></returns>
        public static IReadOnlyDictionary<int, string> CreateRealIdNameMap(IEnumerable<string> files)
        {
            var result = new ConcurrentDictionary<int, string>();

            foreach (var file in files)
            {
                var parser = new EcfFile(file);

                Parallel.ForEach(parser.Entries, entry =>
                {
                    result.AddOrUpdate(entry.RealId, entry.Name, (k, o) => entry.Name);
                });
            }

            return result;
        }

        private IEnumerable<EcfEntry> ParseFile()
        {
            var contents = File.ReadAllText(_file);

            // Replace multiline comments
            contents = Regex.Replace(contents, "/\\*[^*]*\\*+(?:[^/*][^*]*\\*+)*/", "");

            // Replace single line comments, replace with newline to fix end of line parsing
            contents = Regex.Replace(contents, "#(?:.*?)\r?\n", Environment.NewLine);

            // Split & Process lines
            return contents.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                .AsParallel()
                .Select(line => ProcessLine(line))
                .Where(entry => entry != null);
        }

        private static EcfEntry ProcessLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return null;

            var match = Regex.Match(line, @"{ \+?(Item|Block) Id: ([0-9]+), Name: ([^\,\r\n]+)");
            if (match.Success)
            {
                return new EcfEntry()
                {
                    Type = match.Groups[1].Value == "Item" ? EcfEntryType.Item : EcfEntryType.Block,
                    Id = int.Parse(match.Groups[2].Value),
                    Name = match.Groups[3].Value.Trim()
                };
            }

            return null;
        }
    }

    public enum EcfEntryType
    {
        Item,
        Block
    }

    public class EcfEntry
    {
        /// <summary>
        /// The entry type, ex: Item, Block
        /// </summary>
        public EcfEntryType Type { get; set; }

        /// <summary>
        /// The entry Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The entry name, this is typically a unique key for the specified item/block
        /// </summary>
        public string Name { get; set; }

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
}
