using EmpyrionBackpackExtender.NameIdMapping.GameFiles;
using EmpyrionBackpackExtender.NameIdMapping.Tests.Fixtures;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;

namespace EmpyrionBackpackExtender.NameIdMapping.Tests.GameFiles;

public class EcfFileTests : IClassFixture<MockFileSystemFixture>
{
    private readonly IFileSystem _fileSystem;

    public EcfFileTests(MockFileSystemFixture fileSystemFixture) 
        => _fileSystem = fileSystemFixture.NewFileSystem();

    // Theory Data
    public static TheoryData<EcfEntry> BlockData => new()
    {
        new EcfEntry(Type: EcfEntryType.Block, Id: 380,  Name: "HullSmallBlocks"),
        new EcfEntry(Type: EcfEntryType.Block, Id: 1478, Name: "PlasticSmallBlocks" ),
        new EcfEntry(Type: EcfEntryType.Block, Id: 1677, Name: "CargoContainerMedium" ),
        new EcfEntry(Type: EcfEntryType.Block, Id: 628,  Name: "CPUExtenderLargeT5" ),
    };

    public static TheoryData<EcfEntry> ItemEntries => new()
    {
        new EcfEntry(Type: EcfEntryType.Item, Id :5,    Name: "Minigun"),
        new EcfEntry(Type: EcfEntryType.Item, Id :605,  Name: "ArmorHeavyEpic"),
        new EcfEntry(Type: EcfEntryType.Item, Id :1642, Name: "ZiraxTurretPlasmaChargeT2"),
        new EcfEntry(Type: EcfEntryType.Item, Id :3116, Name: "Eden_MinigunIncendiary")
    };

    // Tests
    [Theory]
    [MemberData(nameof(BlockData))]
    public void TestBlockFile(EcfEntry entry)
    {
        var path = BuildEcfFilePath("BlocksConfig.ecf");
        var ecfFile = new EcfFile(_fileSystem, path);

        Assert.NotEmpty(ecfFile.Entries);
        Assert.Contains(entry, ecfFile.Entries, EcfEntryComparer.Instance);
    }

    [Theory]
    [MemberData(nameof(ItemEntries))]
    public void TestItemFile(EcfEntry entry)
    {
        var path = BuildEcfFilePath("ItemsConfig.ecf");
        var ecfFile = new EcfFile(_fileSystem, path);

        Assert.NotEmpty(ecfFile.Entries);
        Assert.Contains(entry, ecfFile.Entries, EcfEntryComparer.Instance);
    }

    [Theory]
    [MemberData(nameof(BlockData))]
    [MemberData(nameof(ItemEntries))]
    public void TestRealIdNameMap(EcfEntry entry)
    {
        var paths = new[]
        {
            BuildEcfFilePath("BlocksConfig.ecf"),
            BuildEcfFilePath("ItemsConfig.ecf")
        };

        var map = EcfFile.CreateRealIdNameMap(_fileSystem, paths);

        Assert.NotEmpty(map);
        Assert.Contains(entry.RealId, map);
        Assert.Equal(entry.Name, map[entry.RealId]);
    }

    // Utils
    private static string BuildEcfFilePath(string fileName)
    {
        return Path.Combine(MockData.ScenarioContentDirectory, "Configuration", fileName);
    }

    private class EcfEntryComparer : IEqualityComparer<EcfEntry>
    {
        public static IEqualityComparer<EcfEntry> Instance { get; } = new EcfEntryComparer();

        public bool Equals(EcfEntry? x, EcfEntry? y)
        {
            return x?.Type == y?.Type && x?.Id == y?.Id && x?.Name == y?.Name;

        }

        public int GetHashCode([DisallowNull] EcfEntry obj)
        {
            return Tuple.Create(obj.Type, obj.Id, obj.Name).GetHashCode();
        }
    }
}
