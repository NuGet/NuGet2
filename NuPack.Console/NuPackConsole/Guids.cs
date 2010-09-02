using System;

namespace NuPackConsole.Implementation
{
    static class GuidList
    {
        public const string guidPowerConsolePkgString = "c0dc615b-6157-4f7f-83fd-90101c4d07d5";
        public const string guidPowerConsoleCmdSetString = "85e8bbe8-ea86-4f38-bd91-619e90f52bc8";
        public const string guidToolWindowPersistanceString = "bcefcbd7-7f08-4701-875b-0083cb834db5";

        public static readonly Guid guidPowerConsoleCmdSet = new Guid(guidPowerConsoleCmdSetString);
    };
}