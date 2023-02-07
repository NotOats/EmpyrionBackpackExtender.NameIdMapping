using EmpyrionBackpackExtender.NameIdMapping.BackpackExtender;
using EmpyrionBackpackExtender.NameIdMapping.GameFiles;
using EmpyrionBackpackExtender.NameIdMapping.Tests.Fixtures;
using System.IO.Abstractions;

namespace EmpyrionBackpackExtender.NameIdMapping.Tests.BackpackExtender;

public class BackpackConfigTests : IClassFixture<MockFileSystemFixture>
{
    private readonly IFileSystem _fileSystem;
    public BackpackConfigTests(MockFileSystemFixture fileSystemFixture)
        => _fileSystem = fileSystemFixture.NewFileSystem();

    [Fact]
    public void Test_LoadFromFile()
    {
        var file = _fileSystem.Path.Join(MockData.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender\Configuration.json");
        var config = new BackpackConfig(_fileSystem, file);

        Assert.Equal("NameIdMapping.json", config.NameIdMappingFile);
        Assert.Equal(@"Personal\{0}.json", config.PersonalBackpackPattern);
    }

    [Fact]
    public void Test_LoadFromSave()
    {
        var save = new SaveGame(_fileSystem, MockData.GameFilesDirectory, MockData.ServerConfigFile);
        var config = new BackpackConfig(_fileSystem, save);

        Assert.Equal("NameIdMapping.json", config.NameIdMappingFile);
        Assert.Equal(@"Personal\{0}.json", config.PersonalBackpackPattern);
    }

    [Fact]
    public void Test_SaveNameIdMappingFile()
    {
        var file = _fileSystem.Path.Join(MockData.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender\Configuration.json");
        var config = new BackpackConfig(_fileSystem, file);
        var value = "RandomTestValue.json";

        config.NameIdMappingFile = value;
        config.Save();

        FileAssert.Contains(_fileSystem, file, value);
    }
}
