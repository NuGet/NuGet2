using System;

namespace NuPack.Tools
{
    static class GuidList
    {
        public const string guidNuPackPkgString = "c0dc615b-6157-4f7f-83fd-90101c4d07d5";

        public const string guidNuPackConsoleCmdSetString = "85e8bbe8-ea86-4f38-bd91-619e90f52bc8";
        public const string guidNuPackDialogCmdSetString = "25fd982b-8cae-4cbd-a440-e03ffccde106";

        public static readonly Guid guidNuPackConsoleCmdSet = new Guid(guidNuPackConsoleCmdSetString);
        public static readonly Guid guidNuPackDialogCmdSet = new Guid(guidNuPackDialogCmdSetString);
    };
}