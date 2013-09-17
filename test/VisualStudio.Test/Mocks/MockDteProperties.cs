using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Moq;

namespace NuGet.VisualStudio.Test
{
    public class MockDteProperties : Properties
    {
        private Dictionary<object, Property> _properties = new Dictionary<object, Property>();

        public object Application
        {
            get { return null; }
        }

        public int Count
        {
            get { return _properties.Count; }
        }

        public DTE DTE
        {
            get { return null; }
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return _properties.Values.GetEnumerator();
        }

        public Property Item(object index)
        {
            Property item;
            _properties.TryGetValue(index, out item);
            return item;
        }

        public void AddProperty(string name, object value)
        {
            var property = new Mock<Property>();
            property.Setup(p => p.Name).Returns(name);
            property.Setup(p => p.Value).Returns(value);

            _properties[name] = property.Object;
        }

        public object Parent
        {
            get { return null; }
        }
    }
}
