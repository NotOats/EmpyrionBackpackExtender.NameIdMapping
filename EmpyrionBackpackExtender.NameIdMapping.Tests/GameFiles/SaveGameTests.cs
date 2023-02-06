using EmpyrionBackpackExtender.NameIdMapping.GameFiles;
using EmpyrionBackpackExtender.NameIdMapping.Tests.Fixtures;

namespace EmpyrionBackpackExtender.NameIdMapping.Tests.GameFiles;

public class SaveGameTests : IClassFixture<SaveGameFixture>
{
    private SaveGameFixture Fixture { get; }

    public SaveGameTests(SaveGameFixture fixture) 
        => Fixture = fixture;

    [Fact]
    public void TestGameDirectories()
    {
        var config = Fixture.SaveGame;
        Assert.NotNull(config);

        Assert.Equal(MockData.GameFilesDirectory, config.ServerDirectory);
        Assert.Equal(MockData.ContentDirectory, config.ContentDirectory);
        Assert.Equal(MockData.SaveGameDirectory, config.SaveGameDirectory);
        Assert.Equal(MockData.ScenarioContentDirectory, config.ScenarioContentDirectory);
    }

    [Fact]
    public void TestServerConfig()
    {
        var serverConfig = Fixture.SaveGame?.ServerConfig;
        Assert.NotNull(serverConfig);

        Assert.Equal(MockData.ServerName, serverConfig.ServerName);
        Assert.Equal(MockData.SaveDirectoryName, serverConfig.SaveDirectory);
        Assert.Equal(MockData.AdminConfigFile, serverConfig.AdminConfigFile);
        Assert.Equal(MockData.GameName, serverConfig.GameName);
        Assert.Equal(MockData.CustomScenario, serverConfig.CustomScenario);
    }

    // TODO: Extract theory data into it's own class/file
    [Theory]
    [MemberData(nameof(EcfFileTests.BlockData), MemberType = typeof(EcfFileTests))]
    [MemberData(nameof(EcfFileTests.ItemEntries), MemberType = typeof(EcfFileTests))]
    public void TestRealIdNameMap(EcfEntry entry)
    {
        var map = Fixture.RealIdNameMap;
        Assert.NotNull(map);

        Assert.NotEmpty(map);
        Assert.Contains(entry.RealId, map);
        Assert.Equal(entry.Name, map[entry.RealId]);
    }
}
