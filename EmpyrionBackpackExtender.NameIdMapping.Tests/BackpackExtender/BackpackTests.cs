using EmpyrionBackpackExtender.NameIdMapping.BackpackExtender;
using EmpyrionBackpackExtender.NameIdMapping.Tests.Fixtures;
using System.IO.Abstractions;


namespace EmpyrionBackpackExtender.NameIdMapping.Tests.BackpackExtender;

public class BackpackTests : IClassFixture<MockFileSystemFixture>, IClassFixture<SaveGameFixture>
{
    private readonly IFileSystem _fileSystem;
    private readonly SaveGameFixture _saveGameFixture;
    public BackpackTests(MockFileSystemFixture fileSystemFixture, SaveGameFixture saveGameFixture)
    {
        _fileSystem = fileSystemFixture.NewFileSystem();
        _saveGameFixture = saveGameFixture;
    }

    [Fact]
    public void Test_LoadBackpack()
    {
        var backpack = LoadBackpack("full.json");
        Assert.NotNull(backpack);
        
        var data = backpack.BackpackData;
        Assert.NotNull(data);

        Assert.Equal("TestPlayerName", data.LastAccessPlayerName);
        Assert.Equal("Hum", data.LastAccessFactionName);
    }

    [Theory]
    [InlineData("Random Player Name Here", 10000)]
    public void Test_SaveBackpack(string name, int count)
    {
        var GetName = (Backpack bp) => bp.BackpackData.LastAccessPlayerName;
        var GetCount = (Backpack bp) => bp.BackpackData.Backpacks[0].Items[0].count;

        var backpack = LoadBackpack("full.json");
        Assert.NotNull(backpack);

        var previousName = GetName(backpack);
        var previousCount = GetCount(backpack);

        backpack.BackpackData.LastAccessPlayerName = name;
        backpack.BackpackData.Backpacks[0].Items[0].count = count;

        backpack.Save();

        var changed = LoadBackpack("full.json");
        Assert.NotNull(changed);

        Assert.Equal(name, GetName(changed));
        Assert.Equal(count, GetCount(changed));
        Assert.NotEqual(previousName, GetName(changed));
        Assert.NotEqual(previousCount, GetCount(changed));
    }

    [Theory]
    [InlineData("CockpitBlocksSV", 1093)]
    [InlineData("AutoMiningDeviceT2", 1109)]
    [InlineData("Eden_DrillTurretAutoCVT2", 2699)]
    public void Test_ItemsExist(string itemName, int itemId)
    {
        var backpack = LoadBackpack("full.json");
        Assert.NotNull(backpack);

        var data = backpack.BackpackData;
        Assert.NotNull(data);

        var item = data.Backpacks.SelectMany(x => x.Items).FirstOrDefault(x => x.name == itemName);
        Assert.NotNull(item);
        Assert.Equal(itemId, item.id);
    }

    [Fact]
    public void Test_ConvertRealIdsToName()
    {
        var backpack = LoadBackpack("no_names.json");
        Assert.NotNull(backpack);

        var total = backpack.BackpackData.Backpacks.SelectMany(b => b.Items).Count();
        var count = backpack.ConvertRealIdsToName(_saveGameFixture.RealIdNameMap);
        Assert.Equal(total, count);

        var items = backpack.BackpackData.Backpacks.SelectMany(b => b.Items);
        Assert.All(items, i => Assert.False(string.IsNullOrEmpty(i.name), "name is null or empty"));
    }

    [Theory]
    [InlineData("CockpitBlocksSV", "EPIC_CockpitBlocksSV")]
    [InlineData("AutoMiningDeviceT2", "ManualMiningDeviceT2")]
    [InlineData("Eden_DrillTurretAutoCVT2", "Scenario_DrillTurrentAutoCVT2")]
    public void Test_UpdateItemNames(string oldName, string newName)
    {
        var backpack = LoadBackpack("full.json");
        Assert.NotNull(backpack);

        var total = backpack.BackpackData.Backpacks.SelectMany(b => b.Items).Count(i => i.name == oldName);
        var count = backpack.UpdateItemName(oldName, newName);
        Assert.Equal(total, count);

        var items = backpack.BackpackData.Backpacks.SelectMany(b => b.Items);
        Assert.DoesNotContain(items, i => i.name == oldName);

        var newCount = backpack.BackpackData.Backpacks.SelectMany(b => b.Items).Count(i => i.name == newName);
        Assert.Equal(total, newCount);
    }

    private Backpack? LoadBackpack(string file)
    {
        var fullFile = _fileSystem.Path.Join(MockData.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender\Personal", file);
        if (!_fileSystem.File.Exists(fullFile))
            return null;

        return new Backpack(_fileSystem, fullFile, BackpackType.Player);
    }
}
