using System;

namespace NuGet.Tools
{
    static class GuidList
    {
        public const string guidNuGetPkgString = "F7D0E7A3-C60B-422A-BFAE-CEED36ADE7D2";

        public const string guidNuGetConsoleCmdSetString = "1E8A55F6-C18D-407F-91C8-94B02AE1CED6";
        public const string guidNuGetDialogCmdSetString = "25fd982b-8cae-4cbd-a440-e03ffccde106";

        public static readonly Guid guidNuGetConsoleCmdSet = new Guid(guidNuGetConsoleCmdSetString);
        public static readonly Guid guidNuGetDialogCmdSet = new Guid(guidNuGetDialogCmdSetString);
    };
}
