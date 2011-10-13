using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using Microsoft.PowerShell.Commands;
using Xunit;

namespace NuGet.PowerShell.Commands.Test
{
    // use a proxy here to save ourselves having to load the Cmdlets
    // assembly into the ps harness.
    public static class PSPathUtilityProxy
    {
        public static bool TryTranslatePSPath(SessionState session, string psPath, out string path, out bool? exists, out string errorMessage)
        {
            return PSPathUtility.TryTranslatePSPath(session, psPath, out path, out exists, out errorMessage);
        }
    }

    public class PSPathTranslationTests : IDisposable
    {
        private string _tempFilePath;
        private System.Management.Automation.PowerShell _ps;

        private const string ScriptTemplate = @"
            $path = $null
            $exists = $null
            $errorMessage = $null

            # wrap call in hashtable to return results
            @{{success = [nuget.powershell.commands.test.pspathutilityproxy]::trytranslatepspath(
                $executioncontext.sessionstate,
                '{0}', # pspath
                [ref]$path,
                [ref]$exists,
                [ref]$errorMessage);
              path = $path;
              exists = $exists;
              errorMessage = $errorMessage
            }}";

        public PSPathTranslationTests()
        {
            // create temp file
            _tempFilePath = Path.GetTempFileName();

            // initialize 
            var state = InitialSessionState.CreateDefault();
            state.ThreadOptions = PSThreadOptions.UseCurrentThread;
            state.ApartmentState = ApartmentState.STA;
            _ps = System.Management.Automation.PowerShell.Create();
            _ps.Runspace = RunspaceFactory.CreateRunspace(state);
            _ps.Runspace.Open();

            // create a new PSDrive for translation tests
            _ps.AddCommand("New-PSDrive")
               .AddParameter("Name", "mytemp")
               .AddParameter("PSProvider", FileSystemProvider.ProviderName)
               .AddParameter("Root", Path.GetTempPath());
            _ps.Invoke();
            Assert.True(_ps.Streams.Error.Count == 0, "Failed to create mytemp psdrive.");

            _ps.Streams.ClearStreams();
            _ps.Commands.Clear();
        }

        [Fact]
        public void TranslatePSPathThatShouldExist()
        {

            string psPath = "mytemp:\\" + Path.GetFileName(_tempFilePath);

            // test against PSDrive 
            _ps.Commands.AddScript(String.Format(ScriptTemplate, psPath));

            Hashtable result = _ps.Invoke<Hashtable>().SingleOrDefault();

            Assert.NotNull(result);
            Assert.True(_ps.Streams.Error.Count == 0);
            Assert.True((bool)result["success"]);
            Assert.True((bool)result["exists"]);
            Assert.Equal(_tempFilePath, (string)result["path"], StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void TranslatePSPathThatShouldNotExist()
        {

            string randomFile = Path.GetRandomFileName();
            string psPath = "mytemp:\\" + randomFile;
            string win32Path = Path.Combine(Path.GetTempPath(), randomFile);

            _ps.Commands.AddScript(String.Format(ScriptTemplate, psPath));

            Hashtable result = _ps.Invoke<Hashtable>().SingleOrDefault();

            Assert.NotNull(result);
            Assert.True(_ps.Streams.Error.Count == 0);
            Assert.True((bool)result["success"]);
            Assert.False((bool)result["exists"]);
            Assert.Equal(win32Path, (string)result["path"], StringComparer.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            _ps.Dispose();

            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }
    }
}
