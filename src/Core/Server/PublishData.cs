using System.Runtime.Serialization;

namespace NuGet
{
    [DataContract]
    public class PublishData
    {
        [DataMember(Name = "key")]
        public string Key { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }
    }
}
