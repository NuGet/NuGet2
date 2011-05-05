using System;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGetConsole;
using NuGetConsole.Host.PowerShell.Implementation;
using NuGetConsole.Implementation.Console;

namespace PowerShellHost.Test {
    [TestClass]
    public class KeyProcessingTest {
        private static void InitializeConsole(
            out Mock<NuGetRawUserInterface> mockRawUI,
            out Mock<NuGetHostUserInterface> mockUI,
            out ConsoleDispatcher dispatcher) {

            var console = new Mock<IConsole>();
            console.Setup(o => o.Write("text"));
            console.Setup(o => o.Write("text", null, null));

            var privateWpfConsole = new Mock<IPrivateWpfConsole>();
            dispatcher = new ConsoleDispatcher(privateWpfConsole.Object);
            console.SetupGet(o => o.Dispatcher).Returns(dispatcher);

            var host = new NuGetPSHost("Test") {ActiveConsole = console.Object};

            mockRawUI = new Mock<NuGetRawUserInterface>(host);
            mockRawUI.CallBase = true;

            mockUI = new Mock<NuGetHostUserInterface>(host);
            mockUI.CallBase = true;
            mockUI.SetupGet(o => o.RawUI).Returns(mockRawUI.Object);
        }

        private static bool TryQueueCancel(ConsoleDispatcher dispatcher) {
            bool queuedCancel = ThreadPool.QueueUserWorkItem(
                state => {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                    dispatcher.CancelWaitKey();
                });
            return queuedCancel;
        }

        [TestMethod]
        public void SimplePostKeyWaitKey() {

            var privateWpfConsole = new Mock<IPrivateWpfConsole>();
            var dispatcher = new ConsoleDispatcher(privateWpfConsole.Object);

            var postedKey = VsKeyInfo.Create(Key.Z, 'z', 0);
            dispatcher.PostKey(postedKey);

            // queue a cancel operation to prevent test getting "stuck" 
            // should the following WaitKey call fail
            bool queuedCancel = TryQueueCancel(dispatcher);
            Assert.IsTrue(queuedCancel);
            
            // blocking
            VsKeyInfo keyInfo = dispatcher.WaitKey();

            Assert.AreEqual(keyInfo, postedKey);
        }

        [TestMethod]
        public void HostUserInterfaceReadkey() {

            Mock<NuGetRawUserInterface> mockRawUI;
            Mock<NuGetHostUserInterface> mockUI;
            ConsoleDispatcher dispatcher;
            InitializeConsole(out mockRawUI, out mockUI, out dispatcher);

            var postedKey = VsKeyInfo.Create(Key.Z, 'z', 90);
            dispatcher.PostKey(postedKey);

            // queue a cancel operation to prevent test getting "stuck" 
            // should the following ReadKey call fail
            var queuedCancel = TryQueueCancel(dispatcher);
            Assert.IsTrue(queuedCancel);

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
            InitializeConsole(out mockRawUI, out mockUI, out dispatcher);

            var keys = new[] {
                VsKeyInfo.Create(Key.N, 'n', 78),
                VsKeyInfo.Create(Key.U, 'u', 85),
                VsKeyInfo.Create(Key.G, 'g', 71),
                VsKeyInfo.Create(Key.E, 'e', 69),
                VsKeyInfo.Create(Key.T, 't', 84),
                VsKeyInfo.Create(Key.Return, '\r', 13) };

            foreach (var key in keys) {
                dispatcher.PostKey(key);
            }

            // queue a cancel operation to prevent test getting "stuck" 
            // should the following ReadKey call fail
            var queuedCancel = TryQueueCancel(dispatcher);
            Assert.IsTrue(queuedCancel);

            string line = mockUI.Object.ReadLine();

            Assert.AreEqual("nuget", line);
        }

        [TestMethod]
        public void HostUserInterfaceReadLineAsSecureString() {
            Mock<NuGetRawUserInterface> mockRawUI;
            Mock<NuGetHostUserInterface> mockUI;
            ConsoleDispatcher dispatcher;
            InitializeConsole(out mockRawUI, out mockUI, out dispatcher);

            var keys = new[] {
                VsKeyInfo.Create(Key.N, 'n', 78),
                VsKeyInfo.Create(Key.U, 'u', 85),
                VsKeyInfo.Create(Key.G, 'g', 71),
                VsKeyInfo.Create(Key.E, 'e', 69),
                VsKeyInfo.Create(Key.T, 't', 84),
                VsKeyInfo.Create(Key.Return, '\r', 13) };

            foreach (var key in keys) {
                dispatcher.PostKey(key);
            }

            // queue a cancel operation to prevent test getting "stuck" 
            // should the following ReadLineAsSecureString call fail
            var queuedCancel = TryQueueCancel(dispatcher);
            Assert.IsTrue(queuedCancel);

            SecureString secure = mockUI.Object.ReadLineAsSecureString();
            
            IntPtr bstr = Marshal.SecureStringToBSTR(secure);
            string line = Marshal.PtrToStringBSTR(bstr);
            Marshal.FreeBSTR(bstr);

            Assert.AreEqual("nuget", line);
        }
    }
}
