using System;

namespace NuGet {
    public interface IVersionSpec {
        SemVer MinVersion { get; }
        bool IsMinInclusive { get; }
        SemVer MaxVersion { get; }
        bool IsMaxInclusive { get; }
    }
}
