using EmpyrionBackpackExtender.NameIdMapping.GameFiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmpyrionBackpackExtender.NameIdMapping;

internal class GameNameIdMap
{
    public IReadOnlyDictionary<string, int> NameIdMap { get; }

    public GameNameIdMap([NotNull] SaveGame save, [NotNull] IEnumerable<string> ecfs)
    {
        var map = save.CreateRealIdToNameMap(ecfs);

        // Change to the correct dictionary found in GitHub-TC/EmpyrionScripting in ConfigEcfAccess.cs 
        // https://github.com/GitHub-TC/EmpyrionScripting/blob/a4f6073e812f38ab5ad90534bf7e2402ac15920d/EmpyrionScripting/ConfigEcfAccess.cs#L16
        // public IReadOnlyDictionary<string, int> BlockIdMapping { get; set; }

        NameIdMap = map.OrderBy(kvp => kvp.Value).ToDictionary(x => x.Value, x => x.Key);
    }

    public void SaveMap( [NotNull] string fileName)
    {
        using StreamWriter writer = File.CreateText(fileName);

        var serializer = JsonSerializer.Create(new JsonSerializerSettings() { Formatting = Formatting.Indented });
        serializer.Serialize(writer, NameIdMap);
    }
}
