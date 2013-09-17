using System.Collections.Generic;
using System.Reflection;
using System;

namespace NuGet.Common
{   
    internal class XBuildSolutionParser : ISolutionParser
    {
        private static MethodInfo GetAllProjectFileNamesMethod()
        {
#pragma warning disable 618
            var assembly = typeof(Microsoft.Build.BuildEngine.Engine).Assembly;
#pragma warning restore 618
            var solutionParserType = assembly.GetType("Mono.XBuild.CommandLine.SolutionParser");
            if (solutionParserType == null)
            {
                throw new CommandLineException(LocalizedResourceManager.GetString("Error_CannotGetXBuildSolutionParser"));
            }

            var methodInfo = solutionParserType.GetMethod(
                "GetAllProjectFileNames", 
                new Type[] {typeof(string) });
            if (methodInfo == null)
            {
                throw new CommandLineException(LocalizedResourceManager.GetString("Error_CannotGetGetAllProjectFileNamesMethod"));
            }

            return methodInfo;
        }

        /// <summary>
        /// Returns the list of project files in the solution file.
        /// </summary>
        /// <param name="fileSystem">The fileSytem. Note that this parameter is ignored and 
        /// has no effect.</param>
        /// <param name="solutionFile">The name of the solution file.</param>
        /// <returns>The list of project files in the solution file.</returns>
        public IEnumerable<string> GetAllProjectFileNames(IFileSystem fileSystem, string solutionFile)
        {
            var getAllProjectFileNamesMethod = GetAllProjectFileNamesMethod();
            var names = (IEnumerable<string>)getAllProjectFileNamesMethod.Invoke(
                null, new object[] { solutionFile });
            return names;
        }
    }
}
