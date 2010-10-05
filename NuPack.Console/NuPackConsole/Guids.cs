using System;

namespace NuPackConsole.Implementation
{
    static class GuidList
    {
        // IMPORTANT: this GUID has to match the one declared in NuPack.Tools\Guid.cs
        public const string guidNuPackConsoleCmdSetString = "1E8A55F6-C18D-407F-91C8-94B02AE1CED6";
        public static readonly Guid guidNuPackCmdSet = new Guid(guidNuPackConsoleCmdSetString);
    };
}