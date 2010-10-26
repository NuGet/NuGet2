using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using System.IO;

namespace NuPack.Test.Integration.MSBuild {
    [TestClass]
    public class NuPackTaskIntegrationTest {
        static string _absolutePackageDir = Path.GetFullPath(@"..\..\..\_package");
        static string _absolutePackageSourceDir = Path.GetFullPath(@"..\..\..\_package_source");
        static string _msBuildPath;
        const string _packageFile = "Fnord.1.2.3.nupkg";
        const string _packageDir = @".\_package";
        const string _packageSourceDir = @".\_package_source";
        const string _workingDir = @".\_working";

        [TestInitialize]
        public void Initialize() {
            DeleteTestDirs();
            
            Directory.CreateDirectory(_absolutePackageDir);
            Directory.CreateDirectory(_absolutePackageSourceDir);
            Directory.CreateDirectory(_packageSourceDir);
            Directory.CreateDirectory(_packageDir);
            Directory.CreateDirectory(_workingDir);
        }

        [TestCleanup]
        public void Cleanup() {
            DeleteTestDirs();
        }

        [TestMethod]
        public void WillCreateAPackageWhenTheSpecFileIsRelativeToTheWorkingDir() {
            string nuPackTaskXml = CreateTaskXml();
            CreatePackageSourceAndBuildFile(nuPackTaskXml);

            string result = ExecuteTask();

            Assert.IsTrue(PackageExists());
        }

        [TestMethod]
        public void WillCreateAPackageWhenThePackageDirIsRelativeToTheWorkingDir() {
            string nuPackTaskXml = CreateTaskXml();
            CreatePackageSourceAndBuildFile(nuPackTaskXml);

            string result = ExecuteTask();

            Assert.IsTrue(PackageExists());
        }

        [TestMethod]
        public void WillCreateAPackageWhenThePackageDirHasAnAbsolutePath() {
            string nuPackTaskXml = CreateTaskXml(packageDir: _absolutePackageDir);
            CreatePackageSourceAndBuildFile(nuPackTaskXml);

            string result = ExecuteTask();

            Assert.IsTrue(PackageExists(packageDir: _absolutePackageDir));
        }

        [TestMethod]
        public void WillCreateAPackageWhenTheSpecFileHasAnAbsolutePath() {
            string nuPackTask = CreateTaskXml(packageSourceDir: _absolutePackageSourceDir);
            CreatePackageSourceAndBuildFile(nuPackTask, packageSourceDir: _absolutePackageSourceDir);

            string result = ExecuteTask();

            Assert.IsTrue(PackageExists());
        }

        [TestMethod]
        public void WillLogAnErrorWhenTheSpecFilesIsMissing() {
            string nuPackTaskXml = "<NuPack />";
            CreatePackageSourceAndBuildFile(nuPackTaskXml, packageSourceDir: _absolutePackageSourceDir);

            string result = ExecuteTask();

            Assert.IsTrue(result.Contains(@"The ""NuPack"" task was not given a value for the required parameter ""SpecFile"""));
            Assert.IsTrue(result.Contains("Build FAILED."));
        }

        [TestMethod]
        public void WillLogAnErrorWhenTheSpecFileIsBad() {
            string nuPackTaskXml = CreateTaskXml(packageSourceDir: "aBadPath");
            CreatePackageSourceAndBuildFile(nuPackTaskXml);

            string result = ExecuteTask();

            Assert.IsTrue(result.Contains("The spec file does not exist"));
            Assert.IsTrue(result.Contains("Build FAILED."));
        }

        [TestMethod]
        public void WillLogAnErrorWhenThePackageDirIsBad() {
            string nuPackTaskXml = CreateTaskXml(packageDir: "aBadPath");
            CreatePackageSourceAndBuildFile(nuPackTaskXml);

            string result = ExecuteTask();

            Assert.IsTrue(result.Contains("The package directory does not exist"));
            Assert.IsTrue(result.Contains("Build FAILED."));
        }

        [TestMethod]
        public void WillLogMessagesWhenAPackageIsCreated() {
            string nuPackTaskXml = CreateTaskXml();
            CreatePackageSourceAndBuildFile(nuPackTaskXml);

            string result = ExecuteTask();

            Assert.IsTrue(result.Contains(
                string.Format(
                    "Creating a package for {0} at {1}.",
                    Path.GetFullPath(Path.Combine(_packageSourceDir, "fnord.nuspec")),
                    Path.GetFullPath(Path.Combine(_packageDir, _packageFile)))));
            Assert.IsTrue(result.Contains(
                string.Format(
                    "Created a package for {0} at {1}.",
                    Path.GetFullPath(Path.Combine(_packageSourceDir, "fnord.nuspec")),
                    Path.GetFullPath(Path.Combine(_packageDir, _packageFile)))));
        }

        [TestMethod]
        public void WillLogErrorWhenAnUnexpectedErrorHappens() {
            File.WriteAllText(Path.Combine(_packageSourceDir, "bad.nuspec"), "badSpecContents");
            string nuPackTaskXml = string.Format(@"<NuPack SpecFile=""..\{0}\bad.nuspec"" />", _packageSourceDir);
            CreatePackageSourceAndBuildFile(nuPackTaskXml);

            string result = ExecuteTask();

            Assert.IsTrue(result.Contains(
                string.Format(
                    "An unexpected error occurred while creating the package:",
                    Path.GetFullPath(Path.Combine(_packageSourceDir, "fnord.nuspec")),
                    Path.GetFullPath(Path.Combine(_packageDir, _packageFile)))));
        }

        static string CreatePackageSourceAndBuildFile(string nuPackTaskXml, string packageSourceDir = _packageSourceDir) {
            var buildFileContents = string.Format(
@"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
    <UsingTask
        AssemblyFile=""..\NuPack.MSBuild.dll""
        TaskName=""NuPack.MSBuild.NuPack"" />
    <Target Name=""Package"">
        {0}
    </Target>
</Project>", nuPackTaskXml);
            var specFileContents =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>Fnord</id>
    <version>1.2.3</version>
    <authors>John Doe</authors>
    <description>Lorem ipsum.</description>
    <language>en-US</language>
  </metadata>
</package>";

            string buildFilePath = Path.Combine(_workingDir, "fnord.msbuild");
            string specFilePath = Path.Combine(packageSourceDir, "fnord.nuspec");

            File.WriteAllText(buildFilePath, buildFileContents);
            File.WriteAllText(specFilePath, specFileContents);

            Directory.CreateDirectory(Path.Combine(packageSourceDir, "lib"));
            File.WriteAllText(Path.Combine(packageSourceDir, "lib", "fnord.dll"), "Not really a .dll");

            return string.Format("{0} {1}", "fnord.msbuild", "/t:Package");
        }

        static string CreateTaskXml(string packageSourceDir = _packageSourceDir, string packageDir = _packageDir) {
            return string.Format(
                @"<NuPack SpecFile=""{0}{1}\fnord.nuspec"" PackageDir=""{2}{3}"" />",
                packageSourceDir.StartsWith(".") ? @"..\" : "",
                packageSourceDir,
                packageDir.StartsWith(".") ? @"..\" : "",
                packageDir);
        }

        static void DeleteTestDirs() {
            DeleteTestDir(_absolutePackageDir);
            DeleteTestDir(_absolutePackageSourceDir);
            DeleteTestDir(_packageDir);
            DeleteTestDir(_packageSourceDir);
            DeleteTestDir(_workingDir);
        }

        static void DeleteTestDir(string dir) {
            string libDir = Path.Combine(dir, "lib");

            if (Directory.Exists(libDir)) {
                foreach (var file in Directory.GetFiles(libDir))
                    File.Delete(file);
                Directory.Delete(libDir);
            }

            if (Directory.Exists(dir)) {
                foreach (var file in Directory.GetFiles(dir))
                    File.Delete(file);
                Directory.Delete(dir);
            }
        }

        static string ExecuteTask() {
            return CommandRunner.Run(
                GetMSBuildPath(),
                _workingDir,
                "fnord.msbuild /v:d /t:Package",
                true).Item2;
        }

        static string GetMSBuildPath() {
            if (_msBuildPath == null) {
                string msBuildDir = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0", "MSBuildToolsPath", null) as string;
                if (msBuildDir == null)
                    throw new Exception("Unable to locate the MSBuild directory.");

                _msBuildPath = Path.Combine(msBuildDir, "MSBuild.exe");
            }

            return _msBuildPath;
        }

        static bool PackageExists(string packageDir = _packageDir) {
            return File.Exists(Path.Combine(packageDir, _packageFile));
        }
    }
}
