using System;
using System.Security;

namespace NuGet
{
    internal class EnvironmentVariableWrapper : IEnvironmentVariableReader
    {
        public static readonly IEnvironmentVariableReader Default = new EnvironmentVariableWrapper();

        private EnvironmentVariableWrapper()
        {
        }

        public string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target)
        {
            try
            {
                return Environment.GetEnvironmentVariable(variable, target);
            }
            catch (SecurityException)
            {
                return null;
            }
        }
    }
}