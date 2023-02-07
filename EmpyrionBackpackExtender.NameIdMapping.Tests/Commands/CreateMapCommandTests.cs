using EmpyrionBackpackExtender.NameIdMapping.Commands;
using EmpyrionBackpackExtender.NameIdMapping.Tests.Fixtures;
using Moq;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using System.IO.Abstractions;


namespace EmpyrionBackpackExtender.NameIdMapping.Tests.Commands;

public class CreateMapCommandTests : IClassFixture<MockFileSystemFixture>
{
    private readonly IFileSystem _fileSystem;
    private readonly TestConsole _console;
    private readonly CreateMapCommand _command;
    private readonly CommandContext _context;
    private readonly CreateMapSettings _settings;

    public CreateMapCommandTests(MockFileSystemFixture fileSystemFixture)
    {
        _fileSystem = fileSystemFixture.NewFileSystem();

        _console = new TestConsole()
            .Colors(ColorSystem.Standard)
            .EmitAnsiSequences();

        _command = new CreateMapCommand(_console, _fileSystem);
        _context = new CommandContext(new Mock<IRemainingArguments>().Object, "create-map", null);
        _settings = new CreateMapSettings
        {
            ServerFolder = MockData.GameFilesDirectory,
            ServerConfig = MockData.ServerConfigFile,
            EcfFiles = string.Join(",", MockData.ItemAndBlockFiles),
            SaveLocal = false,
            SaveServer = null,
            ForceConfigUpdate = false
        };
    }

    [Fact]
    public async void Test_CreateMapFull_Auto()
    {
        _settings.SaveLocal = true;
        _settings.SaveServer = true;
        _settings.ForceConfigUpdate = true;

        var result = await _command.ExecuteAsync(_context, _settings);
        Assert.Equal(0, result);

        var output = _console.Output;
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
        _settings.SaveLocal = true;
        _settings.SaveServer = false;

        var result = await _command.ExecuteAsync(_context, _settings);
        Assert.Equal(0, result);

        Assert.Contains("NameIdMapping.json saved to current directory.", _console.Output);

        FileAssert.Exists(_fileSystem, "NameIdMapping.json");
        FileAssert.Contains(_fileSystem, "NameIdMapping.json", "HullSmallBlocks");
        FileAssert.Contains(_fileSystem, "NameIdMapping.json", "Minigun");
    }

    [Fact]
    public async void Test_CreateMapServer_Auto()
    {
        _settings.SaveServer = true;
        _settings.ForceConfigUpdate = true;

        var result = await _command.ExecuteAsync(_context, _settings);
        Assert.Equal(0, result);

        Assert.Contains("EmpyrionBackpackExtender configuration updated.", _console.Output);

        var serverFile = _fileSystem.Path.Combine(MockData.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender\NameIdMapping.json");
        FileAssert.Exists(_fileSystem, serverFile);
        FileAssert.Contains(_fileSystem, serverFile, "HullSmallBlocks");
        FileAssert.Contains(_fileSystem, serverFile, "Minigun");
    }

    [Fact]
    public async void Test_SaveGameNotFound()
    {
        _settings.SaveServer = true;
        _settings.ForceConfigUpdate = true;

        var configFile = _fileSystem.Path.Combine(_settings.ServerFolder!, _settings.ServerConfig!);
        _fileSystem.File.Delete(configFile);

        var result = await _command.ExecuteAsync(_context, _settings);
        Assert.Equal(2, result);

        Assert.Contains("Config file does not exist at", _console.Output);
    }

    [Fact]
    public async void Test_BackpackConfigNotFound()
    {
        _settings.SaveServer = true;
        _settings.ForceConfigUpdate = true;

        var configFile = _fileSystem.Path.Combine(MockData.SaveGameDirectory, @"Mods\EmpyrionBackpackExtender\Configuration.json");
        _fileSystem.File.Delete(configFile);

        var result = await _command.ExecuteAsync(_context, _settings);
        Assert.Equal(2, result);

        var output = _console.Output;
        Assert.Contains("Generated Real Id <-> Name map with", output);
        Assert.Contains("Config file does not exist at", output);
    }
}
