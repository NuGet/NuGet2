namespace NuGet {

    using System;
    using System.Globalization;

    [Serializable]
    public class CommandLineException : Exception {
        public CommandLineException(string message)
            : base(message) { }

        public CommandLineException(string format,
                                    params object[] args)
            : base(String.Format(CultureInfo.CurrentCulture, format, args)) { }
    }
}
