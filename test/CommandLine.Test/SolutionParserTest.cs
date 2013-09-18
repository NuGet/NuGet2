using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class SolutionParserTest
    {
        [Fact]
        public void XBuildSolutionParserTest()
        {
            if (!EnvironmentUtility.IsMonoRuntime)
            {
                return;
            }

            // Arrange
            var tempFile = Path.GetTempFileName();
            var tempDirectory = Path.GetDirectoryName(tempFile);
            using (StreamWriter writer = new StreamWriter(tempFile))
            {
                writer.Write(
                @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 2012
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""ConsoleApplication1"", ""ConsoleApplication1\ConsoleApplication1.csproj"", ""{034F35C6-790F-4521-9F7C-78C7BC873D75}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Solution Items"", ""Solution Items"", ""{991B31D1-9B1A-4547-81C2-B1133753EDAA}""
	ProjectSection(SolutionItems) = preProject
		TextFile1.txt = TextFile1.txt
	EndProjectSection
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""NewFolder1"", ""NewFolder1"", ""{80CA2398-315E-4C1C-AC79-D25E4AD99A6B}""
	ProjectSection(SolutionItems) = preProject
		TextFile2.txt = TextFile2.txt
	EndProjectSection
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""NewFolder2"", ""NewFolder2"", ""{9C49C3BE-EC45-42C3-8A4A-D5942E665385}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""ClassLibrary1"", ""ClassLibrary1\ClassLibrary1.csproj"", ""{85892561-23F3-413D-86D2-14272C78CB45}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{034F35C6-790F-4521-9F7C-78C7BC873D75}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{034F35C6-790F-4521-9F7C-78C7BC873D75}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{034F35C6-790F-4521-9F7C-78C7BC873D75}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{034F35C6-790F-4521-9F7C-78C7BC873D75}.Release|Any CPU.Build.0 = Release|Any CPU
		{85892561-23F3-413D-86D2-14272C78CB45}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{85892561-23F3-413D-86D2-14272C78CB45}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{85892561-23F3-413D-86D2-14272C78CB45}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{85892561-23F3-413D-86D2-14272C78CB45}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(NestedProjects) = preSolution
		{9C49C3BE-EC45-42C3-8A4A-D5942E665385} = {80CA2398-315E-4C1C-AC79-D25E4AD99A6B}
		{85892561-23F3-413D-86D2-14272C78CB45} = {9C49C3BE-EC45-42C3-8A4A-D5942E665385}
	EndGlobalSection
EndGlobal");
            }

            try
            {
                // Act
                var solutionParser = new XBuildSolutionParser();
                var projects = solutionParser.GetAllProjectFileNames(null, tempFile).ToList();
                projects.Sort();

                // Assert
                Assert.Equal(
                    new[] {
                        Path.Combine(tempDirectory, "ClassLibrary1/ClassLibrary1.csproj"),
                        Path.Combine(tempDirectory, "ConsoleApplication1/ConsoleApplication1.csproj")
                    },
                    projects);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void MSBuildSolutionParserTest()
        {
            if (EnvironmentUtility.IsMonoRuntime)
            {
                return;
            }

            // Arrange
            var fileSystem = new MockFileSystem(@"c:\root");
            fileSystem.AddFile("a.sln",
                @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 2012
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""ConsoleApplication1"", ""ConsoleApplication1\ConsoleApplication1.csproj"", ""{034F35C6-790F-4521-9F7C-78C7BC873D75}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Solution Items"", ""Solution Items"", ""{991B31D1-9B1A-4547-81C2-B1133753EDAA}""
	ProjectSection(SolutionItems) = preProject
		TextFile1.txt = TextFile1.txt
	EndProjectSection
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""NewFolder1"", ""NewFolder1"", ""{80CA2398-315E-4C1C-AC79-D25E4AD99A6B}""
	ProjectSection(SolutionItems) = preProject
		TextFile2.txt = TextFile2.txt
	EndProjectSection
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""NewFolder2"", ""NewFolder2"", ""{9C49C3BE-EC45-42C3-8A4A-D5942E665385}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""ClassLibrary1"", ""ClassLibrary1\ClassLibrary1.csproj"", ""{85892561-23F3-413D-86D2-14272C78CB45}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{034F35C6-790F-4521-9F7C-78C7BC873D75}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{034F35C6-790F-4521-9F7C-78C7BC873D75}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{034F35C6-790F-4521-9F7C-78C7BC873D75}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{034F35C6-790F-4521-9F7C-78C7BC873D75}.Release|Any CPU.Build.0 = Release|Any CPU
		{85892561-23F3-413D-86D2-14272C78CB45}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{85892561-23F3-413D-86D2-14272C78CB45}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{85892561-23F3-413D-86D2-14272C78CB45}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{85892561-23F3-413D-86D2-14272C78CB45}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(NestedProjects) = preSolution
		{9C49C3BE-EC45-42C3-8A4A-D5942E665385} = {80CA2398-315E-4C1C-AC79-D25E4AD99A6B}
		{85892561-23F3-413D-86D2-14272C78CB45} = {9C49C3BE-EC45-42C3-8A4A-D5942E665385}
	EndGlobalSection
EndGlobal");

            // Act
            var solutionParser = new MSBuildSolutionParser();
            var projects = solutionParser.GetAllProjectFileNames(fileSystem, "a.sln").ToList();
            projects.Sort();

            // Assert
            Assert.Equal(
                new[] {
                    @"c:\root\ClassLibrary1\ClassLibrary1.csproj",
                    @"c:\root\ConsoleApplication1\ConsoleApplication1.csproj"
                },
                projects);
        }
    }
}
