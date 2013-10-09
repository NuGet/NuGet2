using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.WebMatrix.Core;

namespace NuGet.WebMatrix
{
    /// <summary>
    /// Commands for the NuGet Gallery
    /// </summary>
    public static class NuGetCommands
    {
        internal const string GroupIdString = "CFEE9892-CBBE-4E2E-BAE1-4F32385EA64D";

        public static readonly Guid GroupId = Guid.Parse(GroupIdString);

        [Guid(GroupIdString)]
        public enum Ids
        {
            None = 0,
            OpenNuGetGallery,
        }

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "CommandId is immutable.")]
        public static readonly CommandId OpenNuGetGalleryCommandId = CommandId.CreateCommandId(Ids.OpenNuGetGallery);
    }
}
