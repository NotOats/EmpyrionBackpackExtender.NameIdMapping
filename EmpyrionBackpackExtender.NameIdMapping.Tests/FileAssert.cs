using System.IO.Abstractions;


namespace EmpyrionBackpackExtender.NameIdMapping.Tests
{
    internal static class FileAssert
    {
        public static void Exists(IFileSystem fileSystem, string file)
        {
            if(!fileSystem.File.Exists(file))
                throw new FileNotFoundException("File Not Found", file);
        }

        public static void Contains(IFileSystem fileSystem, string file, string expected)
        {
            Exists(fileSystem, file);

            var contents = fileSystem.File.ReadAllText(file);

            Assert.Contains(expected, contents);
        }
    }
}
