using System;

namespace NuGet {
    public interface IServerPackageMetadata {
        Uri ReportAbuseUrl { get; }
        int DownloadCount { get; }
        int RatingsCount { get; }
        double Rating { get; }
    }
}
