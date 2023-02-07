using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;


namespace EmpyrionBackpackExtender.NameIdMapping.Tests.Fixtures
{
    public class MockFileSystemFixture
    {
        private readonly IDictionary<string, MockFileData> _files;

        public MockFileSystemFixture()
        {
            _files = MockData.ReadMockGameFiles().ToDictionary(
                kvp => kvp.Key, 
                kvp => new MockFileData(kvp.Value));
        }

        public IFileSystem NewFileSystem() => new MockFileSystem(_files);
    }
}
