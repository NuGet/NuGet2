using System;

namespace NuPackConsole.Implementation
{
    static class GuidList
    {
        public const string guidPowerConsolePkgString = "c0dc615b-6157-4f7f-83fd-90101c4d07d5";
        public const string guidPowerConsoleCmdSetString = "85e8bbe8-ea86-4f38-bd91-619e90f52bc8";
        public const string guidToolWindowPersistanceString = "bcefcbd7-7f08-4701-875b-0083cb834db5";
        public const string guidNuPack_Dialog_PackageCmdSetString = "25fd982b-8cae-4cbd-a440-e03ffccde106";

        public static readonly Guid guidPowerConsoleCmdSet = new Guid(guidPowerConsoleCmdSetString);
        public static readonly Guid guidNuPack_Dialog_PackageCmdSet = new Guid(guidNuPack_Dialog_PackageCmdSetString);
    };
}