using System;
using System.Collections.Generic;
using NuGet.Runtime;

namespace NuGet.Test.Mocks
{
    public class MockAssembly : IAssembly
    {
        public MockAssembly()
        {
            References = new List<IAssembly>();
        }

        public string Name
        {
            get;
            set;
        }

        public Version Version
        {
            get;
            set;
        }

        public string PublicKeyToken
        {
            get;
            set;
        }

        public string Culture
        {
            get;
            set;
        }

        public List<IAssembly> References
        {
            get;
            private set;
        }

        public IEnumerable<IAssembly> ReferencedAssemblies
        {
            get
            {
                return References;
            }
        }
    }
}
