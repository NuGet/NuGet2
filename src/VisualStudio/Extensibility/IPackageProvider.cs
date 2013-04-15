using System;
using System.Runtime.InteropServices;

namespace NuGet.VisualStudio
{
    [ComImport]
    [Guid("3B416055-E43B-4D35-83CC-61A415FC795B")]
    public interface IPackageProvider
    {
        string Name { get; }
        string Description { get; }
        string Publisher { get; }
        Uri IconUrl { get; }
        Uri PublisherUrl { get; }
        
        void Invoke();
    }
}