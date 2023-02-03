using EmpyrionBackpackExtender.NameIdMapping.GameFiles;
using EmpyrionBackpackExtender.NameIdMapping.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmpyrionBackpackExtender.NameIdMapping.Tests.GameFiles;

public class SaveGameTest : IClassFixture<SaveGameFixture>
{
    private SaveGameFixture Fixture { get; }

    public SaveGameTest(SaveGameFixture fixture) => Fixture = fixture;

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
    [MemberData(nameof(EcfFileTest.BlockData), MemberType = typeof(EcfFileTest))]
    [MemberData(nameof(EcfFileTest.ItemEntries), MemberType = typeof(EcfFileTest))]
    public void TestRealIdNameMap(EcfEntry entry)
    {
        var map = Fixture.RealIdNameMap;
        Assert.NotNull(map);

        Assert.NotEmpty(map);
        Assert.Contains(entry.RealId, map);
        Assert.Equal(entry.Name, map[entry.RealId]);
    }
}
