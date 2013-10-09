using System;
using System.Diagnostics;
using NuGet;

namespace NuGet.WebMatrix
{
    internal class FeedSource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:FeedSource"/> class.
        /// </summary>
        public FeedSource(string sourceUrl, string name)
        {
            this.SourceUrl = new Uri(sourceUrl);
            this.Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:FeedSource"/> class.
        /// </summary>
        public FeedSource(Uri sourceUrl, string name)
        {
            this.SourceUrl = sourceUrl;
            this.Name = name;
        }

        public bool IsBuiltIn
        {
            get;
            internal set;
        }

        public Uri SourceUrl
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public string FilterTag
        {
            get;
            set;
        }

        public override bool Equals(object obj)
        {
            FeedSource fs = obj as FeedSource;
            if (fs == null)
            {
                return false;
            }
            else
            {
                return this.Name == fs.Name
                    && this.SourceUrl == fs.SourceUrl
                    && this.FilterTag == fs.FilterTag;
            }
        }

        public override int GetHashCode()
        {
            return (this.Name ?? string.Empty).GetHashCode()
                ^ (this.SourceUrl ?? new Uri("/")).GetHashCode()
                ^ (this.FilterTag ?? string.Empty).GetHashCode();
        }

        public static bool operator ==(FeedSource a, FeedSource b)
        {
            return Equals(a, b);
        }

        public static bool operator !=(FeedSource a, FeedSource b)
        {
            return !Equals(a, b);
        }

        private static bool Equals(FeedSource a, FeedSource b)
        {
            if (object.ReferenceEquals(a, b))
            {
                // If both are null, or both are same instance, return true.
                return true;
            }
            else if (object.ReferenceEquals(a, null) ^ object.ReferenceEquals(b, null))
            {
                // If one is null, but not both, return false.
                return false;
            }
            else
            {
                return a.Equals(b);
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
