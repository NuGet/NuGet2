using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.Project
{
    public interface IUnconfiguredProjectServices
    {
        ConfiguredProject SuggestedConfiguredProject { get; }
    }
}
