using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using NuGet.Common;

namespace NuGet.Commands {
    [Export(typeof(ICommand))]
    [Command(typeof(NuGetResources), "spec", "SpecCommandDescription", MaxArgs = 0)]
    public class SpecCommand : ICommand {
        [Option(typeof(NuGetResources), "SpecCommandAssemblyPathDescription", AltName = "a")]
        public string AssemblyPath {
            get;
            set;
        }

        public List<string> Arguments {
            get;
            set;
        }

        public IConsole Console { get; private set; }

        [ImportingConstructor]
        public SpecCommand(IConsole console) {
            if (console == null) {
                throw new ArgumentNullException("console");
            }

            Console = console;
        }

        public void Execute() {
            var builder = new PackageBuilder();
            if (!String.IsNullOrEmpty(AssemblyPath)) {
                // Load the assembly and try to read the attributes from them
                // REVIEW: ReflectionOnlyLoad would probably be better but we have to read attributes
                // using GetCustomAttributeData() which is a bit wonky
                Assembly assembly = Assembly.LoadFrom(AssemblyPath);
                AssemblyName assemblyName = assembly.GetName();
                builder.Id = assemblyName.Name;
                builder.Version = assemblyName.Version;
                builder.Title = GetAttributeValueOrDefault<AssemblyTitleAttribute>(assembly, a => a.Title);
                builder.Description = GetAttributeValueOrDefault<AssemblyDescriptionAttribute>(assembly, a => a.Description);
                string author = GetAttributeValueOrDefault<AssemblyCompanyAttribute>(assembly, a => a.Company);
                if (!String.IsNullOrEmpty(author)) {
                    builder.Authors.Add(author);
                }
            }
            else {
                builder.Id = "Package";
                builder.Version = new Version("1.0");

            }

            builder.Description = builder.Description ?? "Package description";
            if (!builder.Authors.Any()) {
                builder.Authors.Add("Author here");
            }

            string nuspecFile = builder.Id + ".nuspec";
            using (Stream stream = File.Create(nuspecFile)) {
                Manifest.Create(builder).Save(stream);
            }

            Console.WriteLine(NuGetResources.SpecCommandCreatedNuSpec, nuspecFile);
        }

        private string GetAttributeValueOrDefault<T>(Assembly assembly, Func<T, string> selector) where T : Attribute {
            // Get the attribute
            T attribute = assembly.GetCustomAttributes(typeof(T), inherit: false).Cast<T>().FirstOrDefault();

            return attribute != null ? selector(attribute) : null;
        }
    }
}
