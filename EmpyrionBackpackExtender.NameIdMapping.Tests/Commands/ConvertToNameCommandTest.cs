using EmpyrionBackpackExtender.NameIdMapping.Commands;
using EmpyrionBackpackExtender.NameIdMapping.Tests.Fixtures;
using Moq;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmpyrionBackpackExtender.NameIdMapping.Tests.Commands;

public class ConvertToNameCommandTest : IClassFixture<MockFileSystemFixture>
{
    private readonly IFileSystem _fileSystem;
    private readonly TestConsole _console;
    private readonly ConvertToNameCommand _command;
    private readonly CommandContext _context;
    private readonly ConvertToNameSettings _settings;

    public ConvertToNameCommandTest(MockFileSystemFixture fileSystemFixture)
    {
        _fileSystem = fileSystemFixture.NewFileSystem();

        _console = new TestConsole()
            .Colors(ColorSystem.Standard)
            .EmitAnsiSequences();

        _command = new ConvertToNameCommand(_console, _fileSystem);
        _context = new CommandContext(new Mock<IRemainingArguments>().Object, "convert-to-name", null);
        _settings = new ConvertToNameSettings
        {
            ServerFolder = MockData.GameFilesDirectory,
            ServerConfig = MockData.ServerConfigFile,
            EcfFiles = string.Join(",", MockData.ItemAndBlockFiles),
        };
    }

    [Fact]
    public async void Test_ConvertAllBackpacks()
    {
        var result = await _command.ExecuteAsync(_context, _settings);
        Assert.Equal(0, result);

        // Default MockData has these backpacks/update numbers
        var output = _console.Output;
        Assert.Contains("Finished processing 4 player backpacks", output);
        Assert.Contains("1 Player backpacks updated", output);
        Assert.Contains("28 Items updated", output);
    }
}
