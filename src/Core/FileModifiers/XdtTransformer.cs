using System.Collections.Generic;
using Microsoft.Web.XmlTransform;

namespace NuGet
{
    internal class XdtTransformer : IPackageFileTransformer
    {
        public XdtTransformer()
        {
        }

        public void TransformFile(IPackageFile file, string targetPath, IProjectSystem projectSystem)
        {
            PerformXdtTransform(file, targetPath, projectSystem);
        }

        public void RevertFile(IPackageFile file, string targetPath, IEnumerable<IPackageFile> matchingFiles, IProjectSystem projectSystem)
        {
            PerformXdtTransform(file, targetPath, projectSystem);
        }

        private static void PerformXdtTransform(IPackageFile file, string targetPath, IProjectSystem projectSystem)
        {
            if (projectSystem.FileExists(targetPath))
            {
                string content = Preprocessor.Process(file, projectSystem);

                using (var transformation = new XmlTransformation(content, isTransformAFile: false, logger: null))
                {
                    using (var document = new XmlTransformableDocument())
                    {
                        document.PreserveWhitespace = true;
                        
                        // make sure we close the input stream immediately so that we can override 
                        // the file below when we save to it.
                        using (var inputStream = projectSystem.OpenFile(targetPath))
                        {
                            document.Load(inputStream);
                        }

                        bool succeeded = transformation.Apply(document);
                        if (succeeded)
                        {
                            using (var fileStream = projectSystem.CreateFile(targetPath))
                            { 
                                document.Save(fileStream);
                            }
                        }
                    }
                }
            }
        }
    }
}