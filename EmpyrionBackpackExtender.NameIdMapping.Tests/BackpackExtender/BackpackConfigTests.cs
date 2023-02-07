using EmpyrionBackpackExtender.NameIdMapping.BackpackExtender;
using EmpyrionBackpackExtender.NameIdMapping.GameFiles;
using EmpyrionBackpackExtender.NameIdMapping.Tests.Fixtures;
using System.IO.Abstractions;

namespace EmpyrionBackpackExtender.NameIdMapping.Tests.BackpackExtender;

public class BackpackConfigTests : IClassFixture<MockFileSystemFixture>
{
    private readonly MockFileSystemFixture _fileSystemFixture;
    public BackpackConfigTests(MockFileSystemFixture fileSystemFixture)
        => _fileSystemFixture = fileSystemFixture;

    [Fact]
    public void Test_LoadFromFile()
    {
        var fileSystem = _fileSystemFixture.NewFileSystem();
        var file = fileSystem.Path.Join(MockData.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender\Configuration.json");
        var config = new BackpackConfig(fileSystem, file);

        Assert.Equal("NameIdMapping.json", config.NameIdMappingFile);
        Assert.Equal(@"Personal\{0}.json", config.PersonalBackpackPattern);
    }

    [Fact]
    public void Test_LoadFromSave()
    {
        var fileSystem = _fileSystemFixture.NewFileSystem();
        var save = new SaveGame(fileSystem, MockData.GameFilesDirectory, MockData.ServerConfigFile);
        var config = new BackpackConfig(fileSystem, save);

        Assert.Equal("NameIdMapping.json", config.NameIdMappingFile);
        Assert.Equal(@"Personal\{0}.json", config.PersonalBackpackPattern);
    }

    [Fact]
    public void Test_SaveNameIdMappingFile()
    {
        var fileSystem = _fileSystemFixture.NewFileSystem();
        var file = fileSystem.Path.Join(MockData.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender\Configuration.json");
        var config = new BackpackConfig(fileSystem, file);
        var value = "RandomTestValue.json";

        config.NameIdMappingFile = value;
        config.Save();

        FileAssert.Contains(fileSystem, file, value);
    }
}
