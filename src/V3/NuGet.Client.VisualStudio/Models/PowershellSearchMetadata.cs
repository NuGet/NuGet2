using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.Models
{
    /// <summary>
    /// Model for search results shown by PowerShell console search.
    /// *TODOS: Should we extract out ID,version and summary to a base search model ? 
    /// </summary>
    public class PowershellSearchMetadata
    {
        public string Id { get; set; }
        public NuGetVersion Version { get; set; }
        public string Summary { get; set; }
    }
}
