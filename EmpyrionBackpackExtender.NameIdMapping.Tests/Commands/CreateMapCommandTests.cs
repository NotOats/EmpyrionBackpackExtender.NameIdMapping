using EmpyrionBackpackExtender.NameIdMapping.Commands;
using EmpyrionBackpackExtender.NameIdMapping.Tests.Fixtures;
using Moq;
using Spectre.Console;
using Spectre.Console.Cli;
using System.IO.Abstractions;


namespace EmpyrionBackpackExtender.NameIdMapping.Tests.Commands;

public class CreateMapCommandTests : IClassFixture<MockFileSystemFixture>
{
    private readonly IRemainingArguments _remainingArgs = new Mock<IRemainingArguments>().Object;
    private readonly IFileSystem _fileSystem;

    private static CreateMappingSettings DefaultSettings => new()
    {
        ServerFolder = MockData.GameFilesDirectory,
        ServerConfig = MockData.ServerConfigFile,
        EcfFiles = string.Join(",", MockData.ItemAndBlockFiles),
        SaveLocal = false,
        SaveServer = null,
        ForceConfigUpdate = false
    };

    public CreateMapCommandTests(MockFileSystemFixture fileSystemFixture)
    {
        _fileSystem = fileSystemFixture.FileSystem;
    }

    [Fact]
    public async void Test_CreateMapFull_Auto()
    {
        var command = new CreateMapCommand(_fileSystem);
        var context = new CommandContext(_remainingArgs, "create-map", null);
        var settings = DefaultSettings;
        settings.SaveLocal = true;
        settings.SaveServer = true;
        settings.ForceConfigUpdate = true;
        AnsiConsole.Record();

        var result = await command.ExecuteAsync(context, settings);
        Assert.Equal(0, result);

        var output = AnsiConsole.ExportText();
        Assert.Contains("NameIdMapping.json saved to current directory.", output);
        Assert.Contains("EmpyrionBackpackExtender configuration updated.", output);

        FileAssert.Exists(_fileSystem, "NameIdMapping.json");
        FileAssert.Contains(_fileSystem, "NameIdMapping.json", "HullSmallBlocks");
        FileAssert.Contains(_fileSystem, "NameIdMapping.json", "Minigun");

        var serverFile = _fileSystem.Path.Combine(MockData.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender\NameIdMapping.json");
        FileAssert.Exists(_fileSystem, serverFile);
        FileAssert.Contains(_fileSystem, serverFile, "HullSmallBlocks");
        FileAssert.Contains(_fileSystem, serverFile, "Minigun");
    }

    [Fact]
    public async void Test_CreateMapLocal_Auto()
    {
        var command = new CreateMapCommand(_fileSystem);
        var context = new CommandContext(_remainingArgs, "create-map", null);
        var settings = DefaultSettings;
        settings.SaveLocal = true;
        AnsiConsole.Record();

        var result = await command.ExecuteAsync(context, settings);
        Assert.Equal(0, result);

        Assert.Contains("NameIdMapping.json saved to current directory.", AnsiConsole.ExportText());

        FileAssert.Exists(_fileSystem, "NameIdMapping.json");
        FileAssert.Contains(_fileSystem, "NameIdMapping.json", "HullSmallBlocks");
        FileAssert.Contains(_fileSystem, "NameIdMapping.json", "Minigun");
    }

    [Fact]
    public async void Test_CreateMapServer_Auto()
    {
        var command = new CreateMapCommand(_fileSystem);
        var context = new CommandContext(_remainingArgs, "create-map", null);
        var settings = DefaultSettings;
        settings.SaveServer = true;
        settings.ForceConfigUpdate = true;
        AnsiConsole.Record();

        var result = await command.ExecuteAsync(context, settings);
        Assert.Equal(0, result);

        Assert.Contains("EmpyrionBackpackExtender configuration updated.", AnsiConsole.ExportText());

        var serverFile = _fileSystem.Path.Combine(MockData.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender\NameIdMapping.json");
        FileAssert.Exists(_fileSystem, serverFile);
        FileAssert.Contains(_fileSystem, serverFile, "HullSmallBlocks");
        FileAssert.Contains(_fileSystem, serverFile, "Minigun");
    }
}
