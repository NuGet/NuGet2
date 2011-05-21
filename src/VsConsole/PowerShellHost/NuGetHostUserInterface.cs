using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using System.Text;
using System.Windows.Media;

namespace NuGetConsole.Host.PowerShell.Implementation {
    internal class NuGetHostUserInterface : PSHostUserInterface {
        public const ConsoleColor NoColor = (ConsoleColor)(-1);
        private const int VkCodeReturn = 13;
        private readonly object _instanceLock = new object();
        private readonly NuGetPSHost _host;

        private IConsole Console {
            get {
                return _host.ActiveConsole;
            }
        }

        public NuGetHostUserInterface(NuGetPSHost host) {
            UtilityMethods.ThrowIfArgumentNull(host);
            _host = host;
        }

        public override Dictionary<string, PSObject> Prompt(
            string caption, string message, Collection<FieldDescription> descriptions) {
            WriteErrorLine("Prompt not implemented.");
            return null;
        }

        public override int PromptForChoice(
            string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice) {
            WriteErrorLine("PromptForChoice not implemented.");
            return -1;
        }

        public override PSCredential PromptForCredential(
            string caption, string message, string userName, string targetName,
            PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options) {
            WriteErrorLine("PromptForCredential not implemented.");
            return null;
        }

        public override PSCredential PromptForCredential(
            string caption, string message, string userName, string targetName) {
            WriteErrorLine("PromptForCredential not implemented.");
            return null;
        }

        PSHostRawUserInterface _rawUI;
        public override PSHostRawUserInterface RawUI {
            get {
                if (_rawUI == null) {
                    _rawUI = new NuGetRawUserInterface(_host);
                }
                return _rawUI;
            }
        }

        public override string ReadLine() {
            try {
                var builder = new StringBuilder();

                lock (_instanceLock) {
                    KeyInfo keyInfo;
                    while ((keyInfo = RawUI.ReadKey()).VirtualKeyCode != VkCodeReturn) { // {enter}
                        builder.Append(keyInfo.Character);
                        Write(keyInfo.Character.ToString(CultureInfo.CurrentCulture)); // destined for output, so apply culture
                    }
                }
                return builder.ToString();
            }
            finally {
                WriteLine(String.Empty);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "Caller's responsibility to dispose.")]
        public override SecureString ReadLineAsSecureString() {
            try {
                var secureString = new SecureString();

                lock (_instanceLock) {
                    KeyInfo keyInfo;
                    while ((keyInfo = RawUI.ReadKey()).VirtualKeyCode != VkCodeReturn) { // {enter}
                        secureString.AppendChar(keyInfo.Character); // culture is deferred until securestring is decrypted
                        Write("*");
                    }
                    secureString.MakeReadOnly();
                }
                return secureString;
            }
            finally {
                WriteLine(String.Empty);
            }
        }

        static Color[] _consoleColors;

        /// <summary>
        /// Convert a System.ConsoleColor enum to a Color value, or null if c is not a valid enum.
        /// </summary>
        static Color? ToColor(ConsoleColor c) {
            if (_consoleColors == null) {
                // colors copied from hkcu:\Console color table
                _consoleColors = new Color[16]
                {
                    Color.FromRgb(0x00, 0x00, 0x00),
                    Color.FromRgb(0x00, 0x00, 0x80),
                    Color.FromRgb(0x00, 0x80, 0x00),
                    Color.FromRgb(0x00, 0x80, 0x80),
                    Color.FromRgb(0x80, 0x00, 0x00),
                    Color.FromRgb(0x80, 0x00, 0x80),
                    Color.FromRgb(0x80, 0x80, 0x00),
                    Color.FromRgb(0xC0, 0xC0, 0xC0),
                    Color.FromRgb(0x80, 0x80, 0x80),
                    Color.FromRgb(0x00, 0x00, 0xFF),
                    Color.FromRgb(0x00, 0xFF, 0x00),
                    Color.FromRgb(0x00, 0xFF, 0xFF),
                    Color.FromRgb(0xFF, 0x00, 0x00),
                    Color.FromRgb(0xFF, 0x00, 0xFF),
                    Color.FromRgb(0xFF, 0xFF, 0x00),
                    Color.FromRgb(0xFF, 0xFF, 0xFF),
                };
            }

            int i = (int)c;
            if (i >= 0 && i < _consoleColors.Length) {
                return _consoleColors[i];
            }

            return null; // invalid color
        }

        public override void Write(string value) {
            Console.Write(value);
        }

        public override void WriteLine(string value) {
            Console.WriteLine(value);
        }

        void Write(string value, ConsoleColor foregroundColor, ConsoleColor backgroundColor = NoColor) {
            Console.Write(value, ToColor(foregroundColor), ToColor(backgroundColor));
        }

        void WriteLine(string value, ConsoleColor foregroundColor, ConsoleColor backgroundColor = NoColor) {
            // If append \n only, text becomes 1 line when copied to notepad.
            Write(value + Environment.NewLine, foregroundColor, backgroundColor);
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value) {
            Write(value, foregroundColor, backgroundColor);
        }

        public override void WriteDebugLine(string message) {
            WriteLine(message, ConsoleColor.DarkGray);
        }

        public override void WriteErrorLine(string value) {
            WriteLine(value, ConsoleColor.Red);
        }

        public override void WriteProgress(long sourceId, ProgressRecord record) {
            Console.WriteProgress(record.CurrentOperation, record.PercentComplete);
        }

        public override void WriteVerboseLine(string message) {
            WriteLine(message, ConsoleColor.DarkGray);
        }

        public override void WriteWarningLine(string message) {
            WriteLine(message, ConsoleColor.Magenta);
        }
    }
}