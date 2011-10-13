using System;
using System.Threading;
using System.Windows.Input;
using Moq;
using NuGetConsole;
using NuGetConsole.Host.PowerShell.Implementation;
using NuGetConsole.Implementation.Console;
using Xunit;

namespace PowerShellHost.Test
{
    internal static class InteractiveHelper
    {
        internal static NuGetPSHost InitializeConsole(
            out Mock<NuGetRawUserInterface> mockRawUI,
            out Mock<NuGetHostUserInterface> mockUI,
            out ConsoleDispatcher dispatcher)
        {

            var console = new Mock<IConsole>();
            console.Setup(o => o.Write("text"));
            console.Setup(o => o.Write("text", null, null));

            var privateWpfConsole = new Mock<IPrivateWpfConsole>();
            dispatcher = new ConsoleDispatcher(privateWpfConsole.Object);
            console.SetupGet(o => o.Dispatcher).Returns(dispatcher);

            var host = new NuGetPSHost("Test")
            {
                ActiveConsole = console.Object
            };

            mockRawUI = new Mock<NuGetRawUserInterface>(host);
            mockRawUI.CallBase = true;

            mockUI = new Mock<NuGetHostUserInterface>(host);
            mockUI.CallBase = true;
            mockUI.SetupGet(o => o.RawUI).Returns(mockRawUI.Object);

            return host;
        }

        internal static bool TryQueueCancelWaitKey(ConsoleDispatcher dispatcher, TimeSpan timeout)
        {
            bool cancelWasQueued = ThreadPool.QueueUserWorkItem(
                state =>
                {
                    Thread.Sleep(timeout);
                    dispatcher.CancelWaitKey();
                });
            return cancelWasQueued;
        }

        internal static void PostKeys(ConsoleDispatcher dispatcher, string line, bool appendCarriageReturn = false, TimeSpan timeout = default(TimeSpan))
        {
            IntPtr pKeybLayout = NativeMethods.GetKeyboardLayout(0);

            foreach (char keyChar in line)
            {
                short keyScan = NativeMethods.VkKeyScanEx(keyChar, pKeybLayout);
                byte virtualKey = (byte)(keyScan & 0x00ff);

                VsKeyInfo keyInfo = VsKeyInfo.Create(
                    KeyInterop.KeyFromVirtualKey(virtualKey),
                    keyChar,
                    virtualKey);

                dispatcher.PostKey(keyInfo);
            }

            if (appendCarriageReturn)
            {
                dispatcher.PostKey(VsKeyInfo.Enter);
            }

            // queue a cancel operation to prevent test getting "stuck"
            if (timeout != TimeSpan.Zero)
            {
                var cancelWasQueued = TryQueueCancelWaitKey(dispatcher, timeout);
                Assert.True(cancelWasQueued);
            }
        }
    }
}
