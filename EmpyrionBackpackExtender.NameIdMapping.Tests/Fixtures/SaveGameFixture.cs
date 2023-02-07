using EmpyrionBackpackExtender.NameIdMapping.GameFiles;


namespace EmpyrionBackpackExtender.NameIdMapping.Tests.Fixtures;

public class SaveGameFixture
{
    internal SaveGame SaveGame { get; }
    internal IReadOnlyDictionary<int, string> RealIdNameMap { get; }

    public SaveGameFixture()
    {
        var fileSystemFixture = new MockFileSystemFixture();

        SaveGame = new SaveGame(fileSystemFixture.NewFileSystem(), MockData.GameFilesDirectory, MockData.ServerConfigFile);
        RealIdNameMap = SaveGame.CreateRealIdToNameMap(MockData.ItemAndBlockFiles);
    }
}
