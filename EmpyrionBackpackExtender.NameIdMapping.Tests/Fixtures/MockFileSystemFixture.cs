using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmpyrionBackpackExtender.NameIdMapping.Tests.Fixtures
{
    public class MockFileSystemFixture
    {
        internal IFileSystem FileSystem { get; }

        public MockFileSystemFixture()
        {
            var files = MockData.ReadMockGameFiles().ToDictionary(
                kvp => kvp.Key, 
                kvp => new MockFileData(kvp.Value));

            FileSystem = new MockFileSystem(files);
        }
    }
}
