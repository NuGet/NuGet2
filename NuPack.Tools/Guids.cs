using System;

namespace NuPack.Tools
{
    static class GuidList
    {
        public const string guidNuPackPkgString = "F7D0E7A3-C60B-422A-BFAE-CEED36ADE7D2";

        public const string guidNuPackConsoleCmdSetString = "1E8A55F6-C18D-407F-91C8-94B02AE1CED6";
        public const string guidNuPackDialogCmdSetString = "25fd982b-8cae-4cbd-a440-e03ffccde106";

        public static readonly Guid guidNuPackConsoleCmdSet = new Guid(guidNuPackConsoleCmdSetString);
        public static readonly Guid guidNuPackDialogCmdSet = new Guid(guidNuPackDialogCmdSetString);
    };
}