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
    private readonly MockFileSystemFixture _fileSystemFixture;

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
        => _fileSystemFixture = fileSystemFixture;

    [Fact]
    public async void Test_CreateMapFull_Auto()
    {
        var fileSystem = _fileSystemFixture.NewFileSystem();
        var command = new CreateMapCommand(fileSystem);
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

        FileAssert.Exists(fileSystem, "NameIdMapping.json");
        FileAssert.Contains(fileSystem, "NameIdMapping.json", "HullSmallBlocks");
        FileAssert.Contains(fileSystem, "NameIdMapping.json", "Minigun");

        var serverFile = fileSystem.Path.Combine(MockData.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender\NameIdMapping.json");
        FileAssert.Exists(fileSystem, serverFile);
        FileAssert.Contains(fileSystem, serverFile, "HullSmallBlocks");
        FileAssert.Contains(fileSystem, serverFile, "Minigun");
    }

    [Fact]
    public async void Test_CreateMapLocal_Auto()
    {
        var fileSystem = _fileSystemFixture.NewFileSystem();
        var command = new CreateMapCommand(fileSystem);
        var context = new CommandContext(_remainingArgs, "create-map", null);
        var settings = DefaultSettings;
        settings.SaveLocal = true;
        settings.SaveServer = false;
        AnsiConsole.Record();

        var result = await command.ExecuteAsync(context, settings);
        Assert.Equal(0, result);

        Assert.Contains("NameIdMapping.json saved to current directory.", AnsiConsole.ExportText());

        FileAssert.Exists(fileSystem, "NameIdMapping.json");
        FileAssert.Contains(fileSystem, "NameIdMapping.json", "HullSmallBlocks");
        FileAssert.Contains(fileSystem, "NameIdMapping.json", "Minigun");
    }

    [Fact]
    public async void Test_CreateMapServer_Auto()
    {
        var fileSystem = _fileSystemFixture.NewFileSystem();
        var command = new CreateMapCommand(fileSystem);
        var context = new CommandContext(_remainingArgs, "create-map", null);
        var settings = DefaultSettings;
        settings.SaveServer = true;
        settings.ForceConfigUpdate = true;
        AnsiConsole.Record();

        var result = await command.ExecuteAsync(context, settings);
        Assert.Equal(0, result);

        Assert.Contains("EmpyrionBackpackExtender configuration updated.", AnsiConsole.ExportText());

        var serverFile = fileSystem.Path.Combine(MockData.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender\NameIdMapping.json");
        FileAssert.Exists(fileSystem, serverFile);
        FileAssert.Contains(fileSystem, serverFile, "HullSmallBlocks");
        FileAssert.Contains(fileSystem, serverFile, "Minigun");
    }

    [Fact]
    public async void Test_SaveGameNotFound()
    {
        var fileSystem = _fileSystemFixture.NewFileSystem();

        var configFile = fileSystem.Path.Combine(DefaultSettings.ServerFolder!, DefaultSettings.ServerConfig!);
        fileSystem.File.Delete(configFile);

        var command = new CreateMapCommand(fileSystem);
        var context = new CommandContext(_remainingArgs, "create-map", null);
        var settings = DefaultSettings;
        settings.SaveServer = true;
        settings.ForceConfigUpdate = true;
        AnsiConsole.Record();

        var result = await command.ExecuteAsync(context, settings);
        Assert.Equal(2, result);

        Assert.Contains("Config file does not exist at", AnsiConsole.ExportText());
    }

    [Fact]
    public async void Test_BackpackConfigNotFound()
    {
        var fileSystem = _fileSystemFixture.NewFileSystem();

        var configFile = fileSystem.Path.Combine(MockData.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender\Configuration.json");
        fileSystem.File.Delete(configFile);

        var command = new CreateMapCommand(fileSystem);
        var context = new CommandContext(_remainingArgs, "create-map", null);
        var settings = DefaultSettings;
        settings.SaveServer = true;
        settings.ForceConfigUpdate = true;
        AnsiConsole.Record();

        var result = await command.ExecuteAsync(context, settings);
        Assert.Equal(2, result);

        var output = AnsiConsole.ExportText();
        Assert.Contains("Generated Real Id <-> Name map with", output);
        Assert.Contains("Config file does not exist at", output);
    }
}
