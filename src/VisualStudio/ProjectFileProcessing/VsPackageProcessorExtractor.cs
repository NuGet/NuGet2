using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet.VisualStudio
{
    public class VsPackageProcessorExtractor
    {
        public IEnumerable<IProjectFileProcessor> FromManifestFiles(IEnumerable<IPackageManifestFile> files)
        {
            if (files == null)
            {
                yield break;
            }

            foreach (var file in files)
            {
                foreach (var property in file.Properties)
                {
                    switch (property.Name.ToLower())
                    {
                        case VsProjectItemCustomToolSetter.CustomToolPropertyName:
                            var namespaceProperty =
                                file.Properties
                                    .Where(p => p.Name.Equals(VsProjectItemCustomToolSetter.CustomToolNamespacePropertyName, StringComparison.OrdinalIgnoreCase))
                                    .Select(p => p.Value)
                                    .SingleOrDefault();

                            yield return new VsProjectItemCustomToolSetter(file.Source, property.Value, namespaceProperty);

                            break;

                        case VsProjectItemCustomToolSetter.CustomToolNamespacePropertyName:
                            break;

                        default:
                            yield return new VsProjectItemPropertySetter(file.Source, property.Name, property.Value);
                            break;
                    }
                }
            }
        }
    }
}