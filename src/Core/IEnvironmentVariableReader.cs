using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet
{
    public interface IEnvironmentVariableReader
    {
        string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target);
    }
}
