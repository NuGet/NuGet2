using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using Xunit;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    public class NuGetPushCommandTest
    {
        // Tests pushing to a source that is a file system directory.
        [Fact]
        public void PushCommand_PushToFileSystemSource()
        {   
            var tempPath = Path.GetTempPath();
            var packageDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var source = Path.Combine(tempPath, Guid.NewGuid().ToString());

            try
            {
                // Arrange
                Util.CreateDirectory(packageDirectory);
                var packageFileName = Util.CreateTestPackage("testPackage1", "1.1.0", packageDirectory);

                MemoryStream memoryStream = new MemoryStream();
                TextWriter writer = new StreamWriter(memoryStream);
                Console.SetOut(writer);
                
                // Act
                string[] args = new string[] { "push", packageFileName, "-Source", source };
                int r = Program.Main(args);
                writer.Close();

                // Assert
                Assert.Equal(0, r);                
                Assert.True(File.Exists(Path.Combine(source, "testPackage1.1.1.0.nupkg")));
                var output = Encoding.Default.GetString(memoryStream.ToArray());
                Assert.DoesNotContain("WARNING: No API Key was provided", output);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(packageDirectory);
                Util.DeleteDirectory(source);
            }
        }

        // Same as PushCommand_PushToFileSystemSource, except that the directory is specified
        // in unix style.
        [Fact]
        public void PushCommand_PushToFileSystemSourceUnixStyle()
        {            
            var tempPath = Path.GetTempPath();
            var packageDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var source = Path.Combine(tempPath, Guid.NewGuid().ToString());
            source = source.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            try
            {
                // Arrange
                Util.CreateDirectory(packageDirectory);
                var packageFileName = Util.CreateTestPackage("testPackage1", "1.1.0", packageDirectory);

                // Act
                string[] args = new string[] { "push", packageFileName, "-Source", source };
                int r = Program.Main(args);

                // Assert
                Assert.Equal(0, r);
                Assert.True(File.Exists(Path.Combine(source, "testPackage1.1.1.0.nupkg")));
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(packageDirectory);
                Util.DeleteDirectory(source);
            }
        }

        // Same as PushCommand_PushToFileSystemSource, except that the directory is specified
        // in UNC format.
        [Fact]
        public void PushCommand_PushToFileSystemSourceUncStyle()
        {
            // UNC only works in Windows. So skip this test if we're running on Unix, 
            if (Path.DirectorySeparatorChar == '/')
            {
                return;
            }

            var tempPath = Path.GetTempPath();
            var packageDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var source = Path.Combine(tempPath, Guid.NewGuid().ToString());

            try
            {
                // Arrange
                Util.CreateDirectory(packageDirectory);
                var packageFileName = Util.CreateTestPackage("testPackage1", "1.1.0", packageDirectory);
                var uncSource = @"\\localhost\" + source.Replace(':', '$');

                // Act
                string[] args = new string[] { "push", packageFileName, "-Source", uncSource };
                int r = Program.Main(args);

                // Assert
                Assert.Equal(0, r);
                Assert.True(File.Exists(Path.Combine(source, "testPackage1.1.1.0.nupkg")));
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(packageDirectory);
                Util.DeleteDirectory(source);
            }
        }

        // Tests pushing to an http source
        [Fact]
        public void PushCommand_PushToServer()
        {
            var tempPath = Path.GetTempPath();
            var packageDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var mockServerEndPoint = "http://localhost:1234/";

            try
            {
                // Arrange
                Util.CreateDirectory(packageDirectory);
                var packageFileName = Util.CreateTestPackage("testPackage1", "1.1.0", packageDirectory);
                MemoryStream memoryStream = new MemoryStream();
                TextWriter writer = new StreamWriter(memoryStream);
                Console.SetOut(writer);
                string outputFileName = Path.Combine(packageDirectory, "t1.nupkg");

                var server = new MockServer(mockServerEndPoint);
                server.Get.Add("/push", r => "OK");
                server.Put.Add("/push", r =>
                {
                    byte[] buffer = MockServer.GetPushedPackage(r);                    
                    using (var of = new FileStream(outputFileName, FileMode.Create))
                    {
                        of.Write(buffer, 0, buffer.Length);
                    }

                    return HttpStatusCode.Created;
                });
                server.Start();
                
                // Act
                string[] args = new string[] { "push", packageFileName, "-Source", mockServerEndPoint + "push" };
                int ret = Program.Main(args);
                writer.Close();
                server.Stop();

                // Assert
                Assert.Equal(0, ret);
                var output = Encoding.Default.GetString(memoryStream.ToArray());
                Assert.Contains("Your package was pushed.", output);
                AssertFileEqual(packageFileName, outputFileName);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(packageDirectory);
            }
        }

        // Tests that push command can follow redirection correctly.
        [Fact]
        public void PushCommand_PushToServerFollowRedirection()
        {
            var tempPath = Path.GetTempPath();
            var packageDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var mockServerEndPoint = "http://localhost:1234/";

            try
            {
                // Arrange
                Util.CreateDirectory(packageDirectory);
                var packageFileName = Util.CreateTestPackage("testPackage1", "1.1.0", packageDirectory);
                MemoryStream memoryStream = new MemoryStream();
                TextWriter writer = new StreamWriter(memoryStream);
                Console.SetOut(writer);
                string outputFileName = Path.Combine(packageDirectory, "t1.nupkg");

                var server = new MockServer(mockServerEndPoint);
                server.Get.Add("/redirect", r => "OK");
                server.Put.Add("/redirect", r =>
                    new Action<HttpListenerResponse>(
                        res =>
                        {
                            res.Redirect(mockServerEndPoint + "nuget");
                        }));
                server.Put.Add("/nuget", r =>
                {
                    byte[] buffer = MockServer.GetPushedPackage(r);
                    using (var of = new FileStream(outputFileName, FileMode.Create))
                    {
                        of.Write(buffer, 0, buffer.Length);
                    }

                    return HttpStatusCode.Created;
                });
                server.Start();

                // Act
                string[] args = new string[] { "push", packageFileName, "-Source", mockServerEndPoint + "redirect" };
                int ret = Program.Main(args);
                writer.Close();
                server.Stop();

                // Assert
                var output = Encoding.Default.GetString(memoryStream.ToArray());
                Assert.Equal(0, ret);                
                Assert.Contains("Your package was pushed.", output);
                AssertFileEqual(packageFileName, outputFileName);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(packageDirectory);
            }
        }

        // Tests that push command will terminate even when there is an infinite 
        // redirection loop.
        [Fact]
        public void PushCommand_PushToServerWithInfiniteRedirectionLoop()
        {
            var tempPath = Path.GetTempPath();
            var packageDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var mockServerEndPoint = "http://localhost:1234/";

            try
            {
                // Arrange
                Util.CreateDirectory(packageDirectory);
                var packageFileName = Util.CreateTestPackage("testPackage1", "1.1.0", packageDirectory);
                MemoryStream memoryStream = new MemoryStream();
                TextWriter writer = new StreamWriter(memoryStream);
                Console.SetOut(writer);
                Console.SetError(writer);

                var server = new MockServer(mockServerEndPoint);
                server.Get.Add("/redirect", r => "OK");
                server.Put.Add("/redirect", r =>
                    new Action<HttpListenerResponse>(
                        res =>
                        {
                            res.Redirect(mockServerEndPoint + "redirect");
                        }));                
                server.Start();

                // Act
                string[] args = new string[] { "push", packageFileName, "-Source", mockServerEndPoint + "redirect" };
                int ret = Program.Main(args);
                writer.Close();
                server.Stop();

                // Assert
                var output = Encoding.Default.GetString(memoryStream.ToArray());
                Assert.NotEqual(0, ret);
                Assert.Contains("Too many automatic redirections were attempted.", output);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(packageDirectory);
            }
        }

        // Tests that push command generates error when it detects invalid redirection location.
        [Fact]
        public void PushCommand_PushToServerWithInvalidRedirectionLocation()
        {
            var tempPath = Path.GetTempPath();
            var packageDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var mockServerEndPoint = "http://localhost:1234/";

            try
            {
                // Arrange
                Util.CreateDirectory(packageDirectory);
                var packageFileName = Util.CreateTestPackage("testPackage1", "1.1.0", packageDirectory);
                MemoryStream memoryStream = new MemoryStream();
                TextWriter writer = new StreamWriter(memoryStream);
                Console.SetOut(writer);
                Console.SetError(writer);

                var server = new MockServer(mockServerEndPoint);
                server.Get.Add("/redirect", r => "OK");
                server.Put.Add("/redirect", r => HttpStatusCode.Redirect);
                server.Start();

                // Act
                string[] args = new string[] { "push", packageFileName, "-Source", mockServerEndPoint + "redirect" };
                int ret = Program.Main(args);
                writer.Close();
                server.Stop();

                // Assert
                var output = Encoding.Default.GetString(memoryStream.ToArray());
                Assert.NotEqual(0, ret);
                Assert.Contains("The remote server returned an error: (302)", output);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(packageDirectory);
            }
        }

        // Regression test for the bug that "nuget.exe push" will retry forever instead of asking for 
        // user's password when NuGet.Server uses Windows Authentication.
        [Fact]
        public void PushCommand_PushToServerWontRetryForever()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDir"];
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
            var tempPath = Path.GetTempPath();
            var packageDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var mockServerEndPoint = "http://localhost:1234/";

            try
            {
                // Arrange
                Util.CreateDirectory(packageDirectory);
                var packageFileName = Util.CreateTestPackage("testPackage1", "1.1.0", packageDirectory);
                MemoryStream memoryStream = new MemoryStream();
                TextWriter writer = new StreamWriter(memoryStream);
                Console.SetOut(writer);
                string outputFileName = Path.Combine(packageDirectory, "t1.nupkg");

                var server = new MockServer(mockServerEndPoint);
                server.Get.Add("/push", r => "OK");
                server.Put.Add("/push", r => new Action<HttpListenerResponse>(
                    response =>
                    {
                        response.AddHeader("WWW-Authenticate", "NTLM");
                        response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    }));
                server.Start();

                // Act
                var args = "push " + packageFileName + 
                    " -Source " + mockServerEndPoint + "push -NonInteractive";
                var r1 = CommandRunner.Run(
                    nugetexe,
                    packageDirectory,
                    args,
                    waitForExit: true,
                    timeOutInMilliseconds: 10000);                
                server.Stop();

                // Assert
                Assert.NotEqual(0, r1.Item1);
                Assert.Contains("Please provide credentials for:", r1.Item2);
                Assert.Contains("UserName:", r1.Item2);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(packageDirectory);
            }
        }

        // Asserts that the contents of two files are equal.
        void AssertFileEqual(string fileName1, string fileName2)
        {
            byte[] content1, content2;
            using (var r1 = new FileStream(fileName1, FileMode.Open))
            {
                content1 = r1.ReadAllBytes();
            }
            using (var r1 = new FileStream(fileName2, FileMode.Open))
            {
                content2 = r1.ReadAllBytes();                
            }

            Assert.Equal(content1, content2);
        }
    }
}
