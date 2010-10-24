namespace NuPack {
    using System.IO;

    public interface IPackageFile {
        string Path {
            get;
        }

        Stream GetStream();       
    }
}
