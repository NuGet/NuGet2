using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace NuGet.VisualStudio {

    internal static class SerializationHelper {
        
        public static string Serialize<T>(T objectGraph) where T : class {
            if (objectGraph == null) {
                return String.Empty;
            }

            using (MemoryStream stream = new MemoryStream()) {
                DataContractSerializer serializer = new DataContractSerializer(typeof(T));
                serializer.WriteObject(stream, objectGraph);

                stream.Seek(0, SeekOrigin.Begin);
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);

                return Encoding.UTF8.GetString(buffer);
            }
        }

        public static T Deserialize<T>(string serializedString) where T : class {
            if (String.IsNullOrEmpty(serializedString)) {
                return null;
            }

            using (MemoryStream stream = new MemoryStream()) {
                DataContractSerializer serializer = new DataContractSerializer(typeof(T));

                byte[] buffer = Encoding.UTF8.GetBytes(serializedString);
                stream.Write(buffer, 0, buffer.Length);
                stream.Seek(0, SeekOrigin.Begin);
                
                return (T)serializer.ReadObject(stream);
            }
        }
    }
}
