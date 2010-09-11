// Guids.cs
// MUST match guids.h
using System;

namespace NuPack.Dialog {
    static class GuidList {
        public const string guidNuPack_Dialog_PackagePkgString = "d07580fc-4834-4a8f-b13d-469c7932de68";
        public const string guidNuPack_Dialog_PackageCmdSetString = "25fd982b-8cae-4cbd-a440-e03ffccde106";

        public static readonly Guid guidNuPack_Dialog_PackageCmdSet = new Guid(guidNuPack_Dialog_PackageCmdSetString);
    };
}