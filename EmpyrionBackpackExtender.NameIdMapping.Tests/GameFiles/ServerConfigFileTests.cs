using EmpyrionBackpackExtender.NameIdMapping.GameFiles;
using EmpyrionBackpackExtender.NameIdMapping.Tests.Fixtures;
using System.IO.Abstractions;

namespace EmpyrionBackpackExtender.NameIdMapping.Tests.GameFiles;

public class ServerConfigFileTests : IClassFixture<MockFileSystemFixture>
{
    private readonly MockFileSystemFixture _fileSystemFixture;

    public ServerConfigFileTests(MockFileSystemFixture fileSystemFixture)
        => _fileSystemFixture = fileSystemFixture;

    [Fact]
    public void TestLoadServerConfig()
    {
        var fileSystem = _fileSystemFixture.NewFileSystem();
        var path = Path.Combine(MockData.GameFilesDirectory, MockData.ServerConfigFile);
        var config = ServerConfigFile.Load(fileSystem, path);

        Assert.NotNull(config);
        Assert.Equal(MockData.ServerName, config.ServerName);
        Assert.Equal(MockData.SaveDirectoryName, config.SaveDirectory);
        Assert.Equal(MockData.AdminConfigFile, config.AdminConfigFile);
        Assert.Equal(MockData.GameName, config.GameName);
        Assert.Equal(MockData.CustomScenario, config.CustomScenario);
    }
}
