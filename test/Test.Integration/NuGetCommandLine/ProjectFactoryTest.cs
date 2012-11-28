using System;
using System.IO;
using System.Reflection;

using NuGet.Commands;
using Xunit;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    public class ProjectFactoryTest
    {
        [Fact]
        public void ProjectFactoryCanCompareContentsOfReadOnlyFile()
        {
            var us = Assembly.GetExecutingAssembly();
            var sourcePath = us.Location;
            var targetFile = new PhysicalPackageFile { SourcePath = sourcePath };
            var fullPath = sourcePath + "readOnly";
            File.Copy(sourcePath, fullPath);
            File.SetAttributes(fullPath, FileAttributes.ReadOnly);
            try
            {
                var actual = ProjectFactory.ContentEquals(targetFile, fullPath);

                Assert.Equal(true, actual);
            }
            finally
            {
                File.SetAttributes(fullPath, FileAttributes.Normal);
                File.Delete(fullPath);
            }
        }
    }
}
