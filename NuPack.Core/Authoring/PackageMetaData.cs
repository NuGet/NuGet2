using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuPack {
    public class PackageMetaData {
        private DateTime? _createdTime;

        public string Identifier {
            get;
            set;
        }

        public string Description {
            get;
            set;
        }

        public string Title {
            get;
            set;
        }

        public IList<string> Authors {
            get;
            set;
        }

        public string Category {
            get;
            set;
        }

        public Version Version {
            get;
            set;
        }

        public IList<string> Keywords {
            get;
            set;
        }

        public DateTime Created {
            get {
                return _createdTime ?? DateTime.UtcNow;
            }
            set {
                _createdTime = value;
            }
        }

    }
}
