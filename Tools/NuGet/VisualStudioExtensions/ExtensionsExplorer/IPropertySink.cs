namespace Microsoft.VisualStudio.ExtensionsExplorer
{
    using System;
    using System.Collections.Generic;

    public interface IPropertySink
    {
        void SetProperties(IEnumerator<KeyValuePair<string, object>> propertyBag);
    }
}

