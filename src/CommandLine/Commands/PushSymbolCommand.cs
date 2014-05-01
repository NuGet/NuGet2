using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Commands
{
    [Command(typeof(NuGetCommand), "pushSymbol", "PushSymbolCommandDescription",
        MinArgs = 1, MaxArgs = 1)]
    public class PushSymbolCommand : Command
    {
        [Option(typeof(NuGetCommand), "PushSymbolSourceServerDescription")]
        public string SourceServer { get; set; }

        [Option(typeof(NuGetCommand), "PushSymbolSymbolServerDescription")]
        public string SymbolServer { get; set; }

        IPackage _package;
        Dictionary<string, List<string>> _srcFiles;

        public override void ExecuteCommand()
        {
            if (string.IsNullOrEmpty(SourceServer))
            {
                throw new InvalidOperationException(
                    LocalizedResourceManager.GetString("Error_SourceServerIsRequired"));
            }
            if (string.IsNullOrEmpty(SymbolServer))
            {
                throw new InvalidOperationException(
                    LocalizedResourceManager.GetString("Error_SymbolServerIsRequired"));
            }

            string symbolPackage = Arguments[0];

            _package = new ZipPackage(symbolPackage);
            var pdbFiles = _package.GetFiles().Where(
                f => f.Path.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase));
            if (pdbFiles.IsEmpty())
            {
                string errorMessage = LocalizedResourceManager.GetString("Error_NoPdbFilesInPackage");
                throw new InvalidOperationException(errorMessage);
            }

            ProcessSourceFilesInPackage();
            ProcessPdbFilesInPackage();
        }

        private void ProcessSourceFilesInPackage()
        {
            var srcFiles = _package.GetFiles().Where(
                f => f.Path.StartsWith(@"src\", StringComparison.OrdinalIgnoreCase));
            _srcFiles = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in srcFiles)
            {
                string fileName = Path.GetFileName(f.Path);
                if (String.IsNullOrEmpty(fileName))
                {
                    continue;
                }

                List<string> fileList;
                if (!_srcFiles.TryGetValue(fileName, out fileList))
                {
                    fileList = new List<string>();
                    _srcFiles.Add(fileName, fileList);
                }
                fileList.Add(f.Path.Substring(4));
            }

            PushSourceFilesToSourceServer(srcFiles);
        }

        private void PushSourceFilesToSourceServer(IEnumerable<IPackageFile> srcFiles)
        {
            foreach (var f in srcFiles)
            {
                string targetPath = Path.Combine(
                    SourceServer,
                    _package.Id + "." + _package.Version.ToString(),
                    f.Path.Substring(4));

                var targetDirectory = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                using (var fileStream = new FileStream(targetPath, FileMode.Create))
                {
                    using (var inputFileStream = f.GetStream())
                    {
                        inputFileStream.CopyTo(fileStream);
                    }
                }
            }
        }

        private void ProcessPdbFilesInPackage()
        {
            var pdbFiles = _package.GetFiles().Where(
                f => f.Path.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase));
            
            foreach (var f in pdbFiles)
            {
                SourceIndexAndPushPdbFile(f);
            }
        }

        private void SourceIndexAndPushPdbFile(IPackageFile pdbFileInPackage)
        {
            var pdbFileName = Path.GetFileName(pdbFileInPackage.Path);
            var tempDirectory = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);
            try
            {
                // Save the pdb file in the package as a temporary file
                var tempPdbFile = Path.Combine(tempDirectory, pdbFileName);
                using (var inputStream = pdbFileInPackage.GetStream())
                {
                    using (var outputStream = new FileStream(tempPdbFile, FileMode.Create))
                    {
                        inputStream.CopyTo(outputStream);
                    }
                }

                SourceIndexFile(tempPdbFile, pdbFileInPackage.Path);

                // push to symbol server
                var args = string.Format(
                    CultureInfo.InvariantCulture,
                    "add /f \"{0}\" /s \"{1}\" /t {2}",
                    tempPdbFile,
                    SymbolServer,
                    _package.Id + "." + _package.Version.ToString());
                RunCommand("symstore.exe",
                    args,
                    null,
                    null);
            }
            finally
            {
                Directory.Delete(tempDirectory, true);
            }
        }

        private void SourceIndexFile(string pdbFile, string pdbFileNameInPackage)
        {
            List<string> fileNames = GetSourceFileList(pdbFile);
            var sourceIndexFile = CreateSourceIndex(fileNames, pdbFileNameInPackage);
            try
            {
                UpdatePdbFile(pdbFile, sourceIndexFile);
            }
            finally
            {
                File.Delete(sourceIndexFile);
            }
        }

        private string GetCorrespondingFileInPackage(string fullPath)
        {
            var fileName = Path.GetFileName(fullPath);
            List<string> fileList;
            if (!_srcFiles.TryGetValue(fileName, out fileList))
            {
                return null;
            }

            // find the entry in fileList that has the longest match
            string correspondingFile = fileList
                .Where(f => fullPath.EndsWith(f, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => f.Length)
                .FirstOrDefault();
            return correspondingFile;
        }

        private string CreateSourceIndex(List<string> fileNames, string pdbFileName)
        {
            var tempFile = Path.GetTempFileName();
            using (var writer = new StreamWriter(tempFile))
            {
                writer.WriteLine(
@"SRCSRV: ini ------------------------------------------------
VERSION=2
INDEXVERSION=2
VERCTRL=http
SRCSRV: variables ------------------------------------------
SRCSRVVERCTRL=http
SourceServer={0}
HTTP_EXTRACT_TARGET=%SourceServer%\%var2%
SRCSRVTRG=%http_extract_target%
SRCSRVCMD=
SRCSRV: source files ---------------------------------------", SourceServer);

                foreach (var f in fileNames)
                {
                    var fn = GetCorrespondingFileInPackage(f);
                    if (fn == null)
                    {
                        Console.Log(
                            MessageLevel.Warning,
                            LocalizedResourceManager.GetString("Warning_SourceFileNotFoundInSymbolPackage"),
                            f,
                            pdbFileName);
                    }
                    else
                    {
                        writer.WriteLine("{0}*{1}*",
                            f,
                            Path.Combine(
                                _package.Id + "." + _package.Version.ToString(),
                                fn));
                    }
                }

                writer.WriteLine("SRCSRV: end ------------------------------------------------");
            }

            return tempFile;
        }

        private List<string> GetSourceFileList(string pdbFile)
        {
            var args = String.Format(
                CultureInfo.InvariantCulture,
                "-r \"{0}\"",
                pdbFile);
            List<string> fileNames = new List<string>();
            RunCommand("srctool.exe",
                args,
                (obj, eventArgs) =>
                {
                    var line = eventArgs.Data;
                    if (string.IsNullOrEmpty(line))
                    {
                        return;
                    }

                    fileNames.Add(line);
                    if (Console.Verbosity == NuGet.Verbosity.Detailed)
                    {
                        Console.WriteLine("srctool:        {0}", line);
                    }
                },
                null);

            if (fileNames.Count > 0)
            {
                fileNames.RemoveAt(fileNames.Count - 1);
            }

            return fileNames;
        }

        // update pdb file with source index            
        private void UpdatePdbFile(string pdbFile, string sourceIndexFile)
        {
            var args = string.Format(
                CultureInfo.InvariantCulture,
                "-w -p:{0} -i:{1} -s:srcsrv",
                pdbFile,
                sourceIndexFile);

            RunCommand("pdbstr",
                args,
                null,
                null);
        }

        private int RunCommand(
            string file,
            string args,
            DataReceivedEventHandler outputDataReceived,
            DataReceivedEventHandler errorDataReceived)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = file;
                process.StartInfo.Arguments = args;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = outputDataReceived != null;
                process.StartInfo.RedirectStandardError = errorDataReceived != null;
                process.OutputDataReceived += outputDataReceived;
                process.ErrorDataReceived += errorDataReceived;

                Console.WriteLine(ConsoleColor.Yellow,
                    LocalizedResourceManager.GetString("RunningCommand"), file, args);

                process.Start();
                if (process.StartInfo.RedirectStandardOutput)
                {
                    process.BeginOutputReadLine();
                }
                if (process.StartInfo.RedirectStandardError)
                {
                    process.BeginErrorReadLine();
                }
                process.WaitForExit();

                return process.ExitCode;
            }
        }
    }
}
