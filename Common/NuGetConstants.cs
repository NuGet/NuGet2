using System;

namespace NuGet
{
    public static class NuGetConstants
    {
        public static readonly string DefaultFeedUrl = "https://go.microsoft.com/fwlink/?LinkID=230477";
        public static readonly string V1FeedUrl = "https://go.microsoft.com/fwlink/?LinkID=206669";

        public static readonly string DefaultGalleryServerUrl = "http://go.microsoft.com/fwlink/?LinkID=207106";

        public static readonly string DefaultSymbolServerUrl = "http://nuget.gw.symbolsource.org/Public/NuGet";

        // This is temporary until we fix the gallery to have proper first class support for this.
        // The magic unpublished date is 1900-01-01T00:00:00
        public static readonly DateTimeOffset Unpublished = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8));
    }
}
