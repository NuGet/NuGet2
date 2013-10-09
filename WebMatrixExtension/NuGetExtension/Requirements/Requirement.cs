using System;
using System.Text;

namespace NuGet.WebMatrix
{
    internal class Requirement
    {
        private string _requirement;
        private int _position;

        /// <summary>
        /// Construct a new requirement from the provided requirement string.
        /// </summary>
        /// <param name="requirement"></param>
        internal Requirement(string requirement)
        {
            _requirement = requirement;
        }

        /// <summary>
        /// Returns the original requirement specification
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _requirement;
        }

        /// <summary>
        /// Product name -- Windows
        /// </summary>
        public string Product
        {
            get;
            private set;
        }

        /// <summary>
        /// The required product version number.
        /// </summary>
        public Version Version
        {
            get;
            private set;
        }

        /// <summary>
        /// The required product Service Pack version number or null.
        /// </summary>
        public Version ServicePack
        {
            get;
            private set;
        }

        /// <summary>
        /// Named version string or null.
        /// </summary>
        public string NamedVersion
        {
            get;
            private set;
        }

        /// <summary>
        /// Indicates any OS with a higher Marjor.Minor version number meets the requirement.
        /// </summary>
        public bool OrGreater
        {
            get;
            private set;
        }

        /// <summary>
        /// Indicates the required product type -- either Client, Server, or null.
        /// </summary>
        public string Type
        {
            get;
            private set;
        }

        /// <summary>
        /// Indicates the required product architecture -- either x86, x64, or null
        /// </summary>
        public string Architecture
        {
            get;
            private set;
        }

        /// <summary>
        /// Determines if this requirement is met by the provided operating system information.
        /// </summary>
        /// <param name="OSVersion"></param>
        /// <param name="SPVersion"></param>
        /// <param name="type"></param>
        /// <param name="architecture"></param>
        /// <returns></returns>
        public bool IsMet(Version OSVersion, Version SPVersion, string type, string architecture)
        {
            if (Version != null)
            {
                if (OSVersion.Major == Version.Major && OSVersion.Minor == Version.Minor)
                {
                    if (ServicePack == null || SPVersion >= ServicePack)
                    {
                        if (Type == null || type == Type)
                        {
                            if (Architecture == null || architecture == Architecture)
                            {
                                return true;
                            }
                        }
                    }
                }
                else if (OrGreater && OSVersion > Version)
                {
                    if (Type == null || type == Type)
                    {
                        if (Architecture == null || architecture == Architecture)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Parses the provided requirement.
        /// </summary>
        /// <returns></returns>
        public bool Parse()
        {
            SetPosition(0);
            return ParseRequirement();
        }

        /// <summary>
        /// Parses a requirement specification
        /// </summary>
        /// <returns></returns>
        private bool ParseRequirement()
        {
            int position = CurrentPosition();

            if (ParseLiteral("Requires") && ParseRequirementSpecification() && ParseEOI())
            {
                return true;
            }

            RestorePosition(position);
            return false;
        }

        /// <summary>
        /// Parses a requirement specification for any supported products.
        /// </summary>
        /// <returns></returns>
        private bool ParseRequirementSpecification()
        {
            int position = CurrentPosition();

            if (ParseWindowsRequirement())
            {
                return true;
            }

            RestorePosition(position);
            return false;
        }

        /// <summary>
        /// Parse a Windows requirement specification in any accepted form.
        /// </summary>
        /// <returns></returns>
        private bool ParseWindowsRequirement()
        {
            int position = CurrentPosition();

            if (ParseLiteral("Windows") && ParseWindowsSpecification())
            {
                Product = "Windows";
                return true;
            }

            RestorePosition(position);
            return false;
        }

        /// <summary>
        /// Parses either a named or numeric Windows requirement specification.
        /// </summary>
        /// <returns></returns>
        private bool ParseWindowsSpecification()
        {
            int position = CurrentPosition();

            if (ParseWindowsNamedVersionSpecification() || ParseWindowsNumericVersionSpecification())
            {
                return true;
            }

            RestorePosition(position);
            return false;
        }

        /// <summary>
        /// Parses a windows named version specification in the form of
        /// WindowsNamedVersion[Type][Architecture][sp#[.#]][+]
        /// </summary>
        /// <returns></returns>
        private bool ParseWindowsNamedVersionSpecification()
        {
            int position = CurrentPosition();

            if (ParseWindowsNamedVersion())
            {
                ParseWindowsType();
                ParseArchitecture();
                ParseServicePack();
                ParseOrGreater();

                return true;
            }

            RestorePosition(position);
            return false;
        }

        /// <summary>
        /// Parses a named version of windows from the data stream.
        /// We only accept "XP", "Vista", "7", and "8"
        /// </summary>
        /// <returns></returns>
        private bool ParseWindowsNamedVersion()
        {
            int position = CurrentPosition();

            if (ParseLiteral("XP"))
            {
                NamedVersion = "Windows XP";
                Version = new Version(5, 1, 0, 0);
                return true;
            }
            else if (ParseLiteral("Vista"))
            {
                NamedVersion = "Windows Vista";
                Version = new Version(6, 0, 0, 0);
                return true;
            }
            else if (ParseLiteral("7"))
            {
                NamedVersion = "Windows 7";
                Version = new Version(6, 1, 0, 0);
                return true;
            }
            else if (ParseLiteral("8"))
            {
                NamedVersion = "Windows 8";
                Version = new Version(6, 2, 0, 0);
                return true;
            }

            RestorePosition(position);
            return false;
        }

        /// <summary>
        /// Parses a windows numeric version specification in the form of
        /// [Type][Architecture]Version#[.#][sp#[.#]][+]
        /// </summary>
        /// <returns></returns>
        private bool ParseWindowsNumericVersionSpecification()
        {
            int position = CurrentPosition();

            ParseWindowsType();
            ParseArchitecture();

            if (ParseVersion())
            {
                ParseServicePack();
                ParseOrGreater();

                return true;
            }

            RestorePosition(position);
            return false;
        }

        /// <summary>
        /// Parses a windows version from the data stream in the form of Version#[.#]
        /// </summary>
        /// <returns></returns>
        private bool ParseVersion()
        {
            int position = CurrentPosition();

            Version versionNumber;
            if (ParseLiteral("Version") && ParseVersionNumber(out versionNumber))
            {
                Version = versionNumber;
                return true;
            }

            RestorePosition(position);
            return false;
        }

        /// <summary>
        /// Parses a windows product type from the data stream of either "Client" or "Server".
        /// </summary>
        /// <returns></returns>
        private bool ParseWindowsType()
        {
            int position = CurrentPosition();

            if (ParseLiteral("Client"))
            {
                Type = "Client";
                return true;
            }
            else if (ParseLiteral("Server"))
            {
                Type = "Server";
                return true;
            }

            RestorePosition(position);
            return false;
        }

        /// <summary>
        /// Parses an architechture specification form the data stream in hte format of x86 or x64.
        /// </summary>
        /// <returns></returns>
        private bool ParseArchitecture()
        {
            int position = CurrentPosition();

            if (ParseLiteral("x86"))
            {
                Architecture = "x86";
                return true;
            }
            else if (ParseLiteral("x64"))
            {
                Architecture = "x64";
                return true;
            }

            RestorePosition(position);
            return false;
        }

        /// <summary>
        /// Parses the Service Pack specification from the data stream.
        /// </summary>
        /// <returns></returns>
        private bool ParseServicePack()
        {
            int position = CurrentPosition();

            Version versionNumber;
            if (ParseLiteral("sp") && ParseVersionNumber(out versionNumber))
            {
                ServicePack = versionNumber;
                return true;
            }

            RestorePosition(position);
            return false;
        }

        /// <summary>
        /// Parses a version number in the format uintMajor[.uintMinor]
        /// </summary>
        /// <param name="versionNumber"></param>
        /// <returns></returns>
        private bool ParseVersionNumber(out Version versionNumber)
        {
            versionNumber = null;
            int position = CurrentPosition();

            uint major = 0;
            uint minor = 0;
            if (ParseUnsignedInteger(out major))
            {
                if (ParseLiteral("."))
                {
                    if (ParseUnsignedInteger(out minor))
                    {
                        versionNumber = new Version((int)major, (int)minor, 0, 0);
                        return true;
                    }
                }
                else
                {
                    versionNumber = new Version((int)major, 0, 0, 0);
                    return true;
                }
            }

            RestorePosition(position);
            return false;
        }

        /// <summary>
        /// Parses the indicator if the newer operating systems will work.
        /// </summary>
        /// <returns></returns>
        private bool ParseOrGreater()
        {
            int position = CurrentPosition();

            if (ParseLiteral("+"))
            {
                OrGreater = true;
                return true;
            }

            RestorePosition(position);
            return false;
        }

        /// <summary>
        /// Parse an unsigned integer from the data stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool ParseUnsignedInteger(out uint value)
        {
            value = 0;
            int position = CurrentPosition();

            char digit;
            StringBuilder digits = new StringBuilder(10);
            
            while (ParseDigit(out digit))
            {
                digits.Append(digit);
            }

            if (digits.Length > 0 && uint.TryParse(digits.ToString(), out value))
            {
                return true;
            }

            RestorePosition(position);
            return false;
        }

        /// <summary>
        /// Parse a digit from the data stream.
        /// </summary>
        /// <param name="digit"></param>
        /// <returns></returns>
        private bool ParseDigit(out char digit)
        {
            digit = '\0';
            int position = CurrentPosition();

            char ch;
            if (ReadChar(out ch) && char.IsDigit(ch))
            {
                digit = ch;
                return true;
            }

            RestorePosition(position);
            return false;
        }

        /// <summary>
        /// Parse the specified literal (case sensitive) from the data stream.
        /// </summary>
        /// <param name="literal"></param>
        /// <returns></returns>
        private bool ParseLiteral(string literal)
        {
            if (string.IsNullOrEmpty(literal))
            {
                throw new ArgumentException("literal");
            }

            int position = CurrentPosition();

            string text;
            if (ReadString(literal.Length, out text) && string.Compare(text, literal, StringComparison.Ordinal) == 0)
            {
                return true;
            }

            RestorePosition(position);
            return false;
        }

        /// <summary>
        /// Parse end of input
        /// </summary>
        /// <returns></returns>
        private bool ParseEOI()
        {
            if (IsEOI())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the current position in the input stream.
        /// </summary>
        /// <returns></returns>
        private int CurrentPosition()
        {
            return _position;
        }

        /// <summary>
        /// Sets the current position in the input stream.
        /// </summary>
        /// <param name="position"></param>
        private void SetPosition(int position)
        {
            _position = position;
        }

        /// <summary>
        /// Restores a previously saved position as the current position in the input stream.
        /// </summary>
        /// <param name="position"></param>
        private void RestorePosition(int position)
        {
            if (CurrentPosition() != position)
            {
                SetPosition(position);
            }
        }

        /// <summary>
        /// Determines if the current position is at the end of the input stream.
        /// </summary>
        /// <returns></returns>
        private bool IsEOI()
        {
            if (CurrentPosition() == _requirement.Length)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads a string of the specified length from the data stream.
        /// </summary>
        /// <param name="length"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        private bool ReadString(int length, out string str)
        {
            str = null;
            int position = CurrentPosition();

            if (position + length <= _requirement.Length)
            {
                str = _requirement.Substring((int)position, length);
                SetPosition(position + length);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads a single char from the data stream
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private bool ReadChar(out char ch)
        {
            ch = '\0';
            int position = CurrentPosition();

            if (position + 1 <= _requirement.Length)
            {
                ch = _requirement[(int)position];
                SetPosition(position + 1);
                return true;
            }

            return false;
        }
    }
}
