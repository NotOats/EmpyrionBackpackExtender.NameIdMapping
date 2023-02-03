using EmpyrionBackpackExtender.NameIdMapping.GameFiles;


namespace EmpyrionBackpackExtender.NameIdMapping.Tests.GameFiles;

public class ServerConfigFileTest
{
    [Fact]
    public void TestLoadServerConfig()
    {
        var path = Path.Combine(MockData.GameFilesDirectory, MockData.ServerConfigFile);
        var config = ServerConfigFile.Load(path);

        Assert.NotNull(config);
        Assert.Equal(MockData.ServerName, config.ServerName);
        Assert.Equal(MockData.SaveDirectoryName, config.SaveDirectory);
        Assert.Equal(MockData.AdminConfigFile, config.AdminConfigFile);
        Assert.Equal(MockData.GameName, config.GameName);
        Assert.Equal(MockData.CustomScenario, config.CustomScenario);
    }
}
