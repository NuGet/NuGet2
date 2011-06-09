using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGetConsole;
using NuGetConsole.Host.PowerShell.Implementation;
using NuGetConsole.Implementation.Console;

namespace PowerShellHost.Test {
    [TestClass]
    public class KeyProcessingTest {
        [TestMethod]
        public void SimplePostKeyWaitKey() {

            var privateWpfConsole = new Mock<IPrivateWpfConsole>();
            var dispatcher = new ConsoleDispatcher(privateWpfConsole.Object);

            var postedKey = VsKeyInfo.Create(Key.Z, 'z', 0);
            dispatcher.PostKey(postedKey);

            // test key available
            Assert.IsTrue(dispatcher.IsKeyAvailable);

            // queue a cancel operation to prevent test getting "stuck" 
            // should the following WaitKey call fail
            bool cancelWasQueued = InteractiveHelper.TryQueueCancelWaitKey(dispatcher, timeout: TimeSpan.FromSeconds(5));
            Assert.IsTrue(cancelWasQueued);

            // blocking
            VsKeyInfo keyInfo = dispatcher.WaitKey();
            Assert.AreEqual(keyInfo, postedKey);

            // queue should be empty
            Assert.IsFalse(dispatcher.IsKeyAvailable);
        }

        [TestMethod]
        public void HostUserInterfaceReadkey() {

            Mock<NuGetRawUserInterface> mockRawUI;
            Mock<NuGetHostUserInterface> mockUI;
            ConsoleDispatcher dispatcher;
            InteractiveHelper.InitializeConsole(out mockRawUI, out mockUI, out dispatcher);

            var postedKey = VsKeyInfo.Create(Key.Z, 'z', 90);
            dispatcher.PostKey(postedKey);

            // queue a cancel operation to prevent test getting "stuck" 
            // should the following ReadKey call fail
            var cancelWasQueued = InteractiveHelper.TryQueueCancelWaitKey(dispatcher, TimeSpan.FromSeconds(5));
            Assert.IsTrue(cancelWasQueued);

            KeyInfo keyInfo = mockRawUI.Object.ReadKey();

            Assert.AreEqual(keyInfo.Character, 'z');
            Assert.AreEqual(keyInfo.VirtualKeyCode, 90);
            Assert.AreEqual(keyInfo.ControlKeyState, default(ControlKeyStates));
        }

        [TestMethod]
        public void HostUserInterfaceReadLine() {

            Mock<NuGetRawUserInterface> mockRawUI;
            Mock<NuGetHostUserInterface> mockUI;
            ConsoleDispatcher dispatcher;

            InteractiveHelper.InitializeConsole(out mockRawUI, out mockUI, out dispatcher);

            InteractiveHelper.PostKeys(dispatcher, "nuget", appendCarriageReturn: true, timeout: TimeSpan.FromSeconds(5));

            string line = mockUI.Object.ReadLine();

            Assert.AreEqual("nuget", line);
        }


        [TestMethod]
        public void HostUserInterfaceReadLineAsSecureString() {
            Mock<NuGetRawUserInterface> mockRawUI;
            Mock<NuGetHostUserInterface> mockUI;
            ConsoleDispatcher dispatcher;
            InteractiveHelper.InitializeConsole(out mockRawUI, out mockUI, out dispatcher);

            InteractiveHelper.PostKeys(dispatcher, "nuget", appendCarriageReturn: true, timeout: TimeSpan.FromSeconds(5));

            SecureString secure = mockUI.Object.ReadLineAsSecureString();

            IntPtr bstr = Marshal.SecureStringToBSTR(secure);
            string line = Marshal.PtrToStringBSTR(bstr);
            Marshal.FreeBSTR(bstr);

            Assert.AreEqual("nuget", line);
        }

        [TestMethod]
        public void HostUserInterfacePromptForChoice() {
            Mock<NuGetRawUserInterface> mockRawUI;
            Mock<NuGetHostUserInterface> mockUI;
            ConsoleDispatcher dispatcher;
            InteractiveHelper.InitializeConsole(out mockRawUI, out mockUI, out dispatcher);

            var descriptions = new Collection<ChoiceDescription> {
                                         new ChoiceDescription("&Yes"), // 0 (default)
                                         new ChoiceDescription("&No")   // 1
                                     };

            // test choice
            InteractiveHelper.PostKeys(dispatcher, "n", appendCarriageReturn: true,
                timeout: TimeSpan.FromSeconds(5));

            int chosen = mockUI.Object.PromptForChoice("Test", "Test", descriptions, 0);
            Assert.AreEqual(1, chosen);

            // test default choice
            dispatcher.PostKey(VsKeyInfo.Enter);
            chosen = mockUI.Object.PromptForChoice("Test", "Test", descriptions, 0);
            Assert.AreEqual(0, chosen);
        }

        [TestMethod]
        public void HostUserInterfacePromptForConfirm() {
            Mock<NuGetRawUserInterface> mockRawUI;
            Mock<NuGetHostUserInterface> mockUI;
            ConsoleDispatcher dispatcher;

            var host = InteractiveHelper.InitializeConsole(out mockRawUI, out mockUI, out dispatcher);

            Runspace rs = RunspaceFactory.CreateRunspace(host);
            rs.Open();
            using (PowerShell ps = PowerShell.Create()) {
                ps.Runspace = rs;

                // put a "y" on the input queue
                InteractiveHelper.PostKeys(dispatcher, "y", appendCarriageReturn: true,
                    timeout: TimeSpan.FromSeconds(5));

                bool result = ps.AddScript(@"
                    function test-confirm {
                        [cmdletbinding(supportsshouldprocess=$true)]param();
                        $pscmdlet.shouldprocess('do', 'this')
                    }
                    test-confirm -confirm")
                        .Invoke<bool>()
                        .FirstOrDefault();

                // no errors
                Assert.IsTrue(ps.Streams.Error.Count == 0);

                // shouldprocess accepted a "y"
                Assert.IsTrue(result);

                // put a "n" on the input queue
                InteractiveHelper.PostKeys(dispatcher, "n", appendCarriageReturn: true,
                    timeout: TimeSpan.FromSeconds(5));

                // execute confirm again
                ps.Streams.ClearStreams();
                result = ps.Invoke<bool>().FirstOrDefault();

                // no errors
                Assert.IsTrue(ps.Streams.Error.Count == 0);

                // shouldprocess accepted a "n"
                Assert.IsFalse(result);
            }
        }

        [TestMethod]
        public void HostUserInterfacePromptForMissingMandatoryParameters() {
            Mock<NuGetRawUserInterface> mockRawUI;
            Mock<NuGetHostUserInterface> mockUI;
            ConsoleDispatcher dispatcher;

            var host = InteractiveHelper.InitializeConsole(out mockRawUI, out mockUI, out dispatcher);

            Runspace rs = RunspaceFactory.CreateRunspace(host);
            rs.Open();
            using (PowerShell ps = PowerShell.Create()) {
                ps.Runspace = rs;

                // [string]$Name
                InteractiveHelper.PostKeys(dispatcher, "foo", appendCarriageReturn: true);

                // [int]$Count
                InteractiveHelper.PostKeys(dispatcher, "42", appendCarriageReturn: true);

                // [int[]]$Numbers
                InteractiveHelper.PostKeys(dispatcher, "1", appendCarriageReturn: true);
                InteractiveHelper.PostKeys(dispatcher, "2", appendCarriageReturn: true);
                InteractiveHelper.PostKeys(dispatcher, "3", appendCarriageReturn: true);
                dispatcher.PostKey(VsKeyInfo.Enter); // empty line

                Hashtable result =
                    ps.AddScript(@"
                        function test-missing {
                            param(
                                [parameter(mandatory=$true)]
                                [string]$Name,
                                [parameter(mandatory=$true)]
                                [int]$Count,
                                [parameter(mandatory=$true)]
                                [int[]]$Numbers
                            );
                            @{
                                Name = $name;
                                Count = $count;
                                Numbers = $Numbers
                            }
                        }
                        test-missing")
                            .Invoke<Hashtable>()
                            .FirstOrDefault();

                // no errors
                Assert.IsTrue(ps.Streams.Error.Count == 0);

                Assert.IsNotNull(result);
                Assert.AreEqual(result["Name"], "foo");
                Assert.AreEqual(result["Count"], 42);
                Assert.IsTrue(
                    ((int[])result["Numbers"])
                        .SequenceEqual(new[] { 1, 2, 3 }));
            }
        }
    }
}


