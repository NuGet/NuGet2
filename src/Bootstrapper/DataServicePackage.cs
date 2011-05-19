using System.Data.Services.Common;

namespace NuGet {
    [DataServiceKey("Id", "Version")]
    [EntityPropertyMapping("Id", SyndicationItemProperty.Title, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    public class DataServicePackage {

        public string Id {
            get;
            set;
        }

        public string Version {
            get;
            set;
        }
    }
}