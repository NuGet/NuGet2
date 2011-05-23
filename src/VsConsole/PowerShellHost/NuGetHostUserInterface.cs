using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using System.Text;
using System.Windows.Media;

namespace NuGetConsole.Host.PowerShell.Implementation {
    internal class NuGetHostUserInterface : PSHostUserInterface, IHostUISupportsMultipleChoiceSelection {
        public const ConsoleColor NoColor = (ConsoleColor) (-1);
        private const int VkCodeReturn = 13;
        private const int VkCodeBackspace = 8;
        private static Color[] _consoleColors;
        private readonly NuGetPSHost _host;
        private readonly object _instanceLock = new object();
        private PSHostRawUserInterface _rawUI;

        public NuGetHostUserInterface(NuGetPSHost host) {
            UtilityMethods.ThrowIfArgumentNull(host);
            _host = host;
        }

        private IConsole Console {
            get { return _host.ActiveConsole; }
        }

        public override PSHostRawUserInterface RawUI {
            get {
                if (_rawUI == null) {
                    _rawUI = new NuGetRawUserInterface(_host);
                }
                return _rawUI;
            }
        }

        #region IHostUISupportsMultipleChoiceSelection Members

        public Collection<int> PromptForChoice(string caption,
                                               string message,
                                               Collection<ChoiceDescription> choices,
                                               IEnumerable<int> defaultChoices) {
            WriteErrorLine("IHostUISupportsMultipleChoiceSelection.PromptForChoice not implemented.");

            return null;
        }

        #endregion

        private static Type GetFieldType(FieldDescription field) {
            Type type = null;
            if (!String.IsNullOrEmpty(field.ParameterAssemblyFullName)) {
                LanguagePrimitives.TryConvertTo(field.ParameterAssemblyFullName, out type);
            }
            if ((type == null) && !String.IsNullOrEmpty(field.ParameterTypeFullName)) {
                LanguagePrimitives.TryConvertTo(field.ParameterTypeFullName, out type);
            }
            return type;
        }

        public override Dictionary<string, PSObject> Prompt(
            string caption, string message, Collection<FieldDescription> descriptions) {
            if (descriptions == null) {
                throw new ArgumentNullException("descriptions");
            }
            if (descriptions.Count == 0) {
                throw new ArgumentException(
                    Resources.ZeroLengthCollection,"descriptions");
            }

            if (!String.IsNullOrEmpty(caption)) {
                WriteLine(caption);
            }
            if (!String.IsNullOrEmpty(message)) {
                WriteLine(message);
            }

            var results = new Dictionary<string, PSObject>(descriptions.Count);
            int index = 0;

            foreach (FieldDescription description in descriptions) {
                if ((description == null) ||
                    String.IsNullOrEmpty(description.ParameterAssemblyFullName)) {
                    throw new ArgumentException("descriptions[" + index + "]");
                }

                bool cancelled;
                object answer;
                string name = description.Name;

                Type fieldType = GetFieldType(description) ?? typeof (String);

                // collection type?
                if (typeof (IList).IsAssignableFrom(fieldType)) {
                    cancelled = PromptCollection(name, fieldType, out answer);
                }
                else {
                    cancelled = PromptScalar(name, fieldType, out answer);
                }

                if (cancelled) {
                    WriteLine(String.Empty);
                    results.Clear();
                    break;
                }
                results.Add(name, PSObject.AsPSObject(answer));
                index++;
            }

            return results;
        }

        private bool PromptScalar(string name, Type fieldType, out object answer) {
            bool cancelled;

            if (fieldType.Equals(typeof (SecureString))) {
                Write(name + ": ");
                answer = ReadLineAsSecureString();
                cancelled = (answer == null);
            }
            else if (fieldType.Equals(typeof (PSCredential))) {
                answer = this.PromptForCredential(null, null, null, String.Empty);
                cancelled = (answer == null);
            }
            else {
                bool coercable = true;
                string prompt = name + ": ";
                do {
                    if (coercable) {
                        Write(prompt);
                    }
                    else {
                        // last input invalid
                        Write(prompt, ConsoleColor.Red);
                    }
                    string line = ReadLine();
                    cancelled = (line == null);
                    coercable = LanguagePrimitives.TryConvertTo(line, fieldType, out answer);
                } while (!cancelled && !coercable);
            }
            return cancelled;
        }

        private bool PromptCollection(string name, Type fieldType, out object answer) {
            bool cancelled;
            Type elementType = typeof (Object);

            if (fieldType.IsArray) {
                elementType = fieldType.GetElementType();
                // FIXME: zero rank array check?
            }

            var valuesToConvert = new ArrayList();
            bool coercable = true;

            while (true) {
                string prompt = String.Format(CultureInfo.CurrentCulture,
                    "{0}[{1}]: ", name, valuesToConvert.Count);
                if (coercable) {
                    Write(prompt);
                }
                else {
                    // last input invalid
                    Write(prompt, ConsoleColor.Red);
                }

                string input = ReadLine();
                cancelled = (input == null);
                bool inputComplete = String.IsNullOrEmpty(input);

                if (cancelled || inputComplete) {
                    break;
                }

                coercable = LanguagePrimitives.TryConvertTo(input, elementType, out answer);
                if (coercable) {
                    valuesToConvert.Add(answer);
                }
            }

            if (!cancelled) {
                if (!LanguagePrimitives.TryConvertTo(valuesToConvert, elementType, out answer)) {
                    answer = valuesToConvert;
                }
            }
            else {
                answer = null;
            }
            return cancelled;
        }

        public override int PromptForChoice(
            string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice) {
            if (!String.IsNullOrEmpty(caption)) {
                WriteLine(caption);
            }
            if (!String.IsNullOrEmpty(message)) {
                WriteLine(message);
            }

            int chosen = -1;
            do {
                var accelerators = new string[choices.Count];

                for (int index = 0; index < choices.Count; index++) {
                    ChoiceDescription choice = choices[index];
                    string label = choice.Label;
                    int ampIndex = label.IndexOf('&'); // hotkey marker
                    accelerators[index] = String.Empty;

                    // accelerator marker found?
                    if ((ampIndex != -1) &&
                        (ampIndex < (label.Length - 1))) {
                        accelerators[index] = label
                            .Substring(ampIndex + 1, 1) // grab letter after '&'
                            .ToUpper(CultureInfo.CurrentCulture);
                    }
                    Write(String.Format(CultureInfo.CurrentCulture, "[{0}] {1}  ",
                                        accelerators[index],
                                        label.Replace("&", String.Empty)));
                }
                Write(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Resources.PromptForChoiceSuffix,
                        accelerators[defaultChoice]));

                string input = ReadLine();

                switch (input.Length) {
                    case 0:
                        // enter
                        if (defaultChoice == -1) {
                            continue;
                        }
                        chosen = defaultChoice;
                        break;
                    case 1:
                        // single letter accelerator
                        chosen = Array.FindIndex(
                            accelerators,
                            accelerator => accelerator.Equals(
                                input,
                                StringComparison.OrdinalIgnoreCase));
                        break;
                    default:
                        // match against entire label
                        chosen = Array.FindIndex(
                            choices.ToArray(),
                            choice => choice.Label.Equals(
                                input,
                                StringComparison.OrdinalIgnoreCase));
                        break;
                }
            } while (chosen == -1);

            return chosen;
        }

        public override PSCredential PromptForCredential(
            string caption, string message, string userName, string targetName,
            PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options) {
            return NativeMethods.CredUIPromptForCredentials(
                caption,
                message,
                userName,
                targetName,
                allowedCredentialTypes,
                options);
        }

        public override PSCredential PromptForCredential(
            string caption, string message, string userName, string targetName) {
            return PromptForCredential(
                caption,
                message,
                userName,
                targetName,
                PSCredentialTypes.Default,
                PSCredentialUIOptions.Default);
        }

        public override string ReadLine() {
            try {
                var builder = new StringBuilder();

                lock (_instanceLock) {
                    KeyInfo keyInfo;
                    while ((keyInfo = RawUI.ReadKey()).VirtualKeyCode != VkCodeReturn) {
                        // {enter}
                        if (keyInfo.VirtualKeyCode == VkCodeBackspace) {
                            if (builder.Length > 0) {
                                builder.Remove(builder.Length - 1, 1);
                                Console.WriteBackspace();
                            }
                        }
                        else {
                            builder.Append(keyInfo.Character);
                            // destined for output, so apply culture
                            Write(keyInfo.Character.ToString(CultureInfo.CurrentCulture));
                        }
                    }
                }
                return builder.ToString();
            }
            catch (PipelineStoppedException) {
                // ESC
                return null;
            }
            finally {
                WriteLine(String.Empty);
            }
        }

        [SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "Caller's responsibility to dispose.")]
        public override SecureString ReadLineAsSecureString() {
            try {
                var secureString = new SecureString();

                lock (_instanceLock) {
                    KeyInfo keyInfo;
                    while ((keyInfo = RawUI.ReadKey()).VirtualKeyCode != VkCodeReturn) {
                        // {enter}
                        if (keyInfo.VirtualKeyCode == VkCodeBackspace) {
                            if (secureString.Length > 0) {
                                secureString.RemoveAt(secureString.Length - 1);
                                Console.WriteBackspace();
                            }
                        }
                        else {
                            // culture is deferred until securestring is decrypted
                            secureString.AppendChar(keyInfo.Character);
                            Write("*");
                        }
                    }
                    secureString.MakeReadOnly();
                }
                return secureString;
            }
            catch (PipelineStoppedException) {
                // ESC
                return null;
            }
            finally {
                WriteLine(String.Empty);
            }
        }

        /// <summary>
        ///     Convert a System.ConsoleColor enum to a Color value, or null if c is not a valid enum.
        /// </summary>
        private static Color? ToColor(ConsoleColor c) {
            if (_consoleColors == null) {
                // colors copied from hkcu:\Console color table
                _consoleColors = new Color[16] {
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

            var i = (int) c;
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

        private void Write(string value, ConsoleColor foregroundColor, ConsoleColor backgroundColor = NoColor) {
            Console.Write(value, ToColor(foregroundColor), ToColor(backgroundColor));
        }

        private void WriteLine(string value, ConsoleColor foregroundColor, ConsoleColor backgroundColor = NoColor) {
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