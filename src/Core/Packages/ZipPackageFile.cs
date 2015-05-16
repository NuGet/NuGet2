using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Runtime.Versioning;

namespace NuGet
{
    internal class ZipPackageFile : IPackageFile
    {
        private readonly Func<Stream> _streamFactory;
        private readonly FrameworkName _targetFramework;

        public ZipPackageFile(PackagePart part, bool useManagedCodeConventions)
            : this(UriUtility.GetPath(part.Uri), useManagedCodeConventions, part.GetStream().ToStreamFactory())
        {
        }

        public ZipPackageFile(IPackageFile file, bool useManagedCodeConventions)
            : this(file.Path, useManagedCodeConventions, file.GetStream().ToStreamFactory())
        {
        }

        protected ZipPackageFile(string path, bool useManagedCodeConventions, Func<Stream> streamFactory)
        {
            Path = path;
            _streamFactory = streamFactory;

            string effectivePath;
            _targetFramework = VersionUtility.ParseFrameworkNameFromFilePath(
                path,
                useManagedCodeConventions: useManagedCodeConventions,
                effectivePath: out effectivePath);
            EffectivePath = effectivePath;
        }

        public string Path
        {
            get;
            private set;
        }

        public string EffectivePath
        {
            get;
            private set;
        }

        public FrameworkName TargetFramework
        {
            get
            {
                return _targetFramework;
            }
        }

        IEnumerable<FrameworkName> IFrameworkTargetable.SupportedFrameworks
        {
            get
            {
                if (TargetFramework != null)
                {
                    yield return TargetFramework;
                }
                yield break;
            }
        }

        public Stream GetStream()
        {
            return _streamFactory();
        }

        public override string ToString()
        {
            return Path;
        }
    }
}