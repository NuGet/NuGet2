using System;

namespace NuGetConsole.Implementation {
    static class GuidList {
        // IMPORTANT: this GUID has to match the one declared in NuGet.Tools\Guid.cs
        public const string guidNuGetConsoleCmdSetString = "1E8A55F6-C18D-407F-91C8-94B02AE1CED6";
        public static readonly Guid guidNuGetCmdSet = new Guid(guidNuGetConsoleCmdSetString);
    };
}
