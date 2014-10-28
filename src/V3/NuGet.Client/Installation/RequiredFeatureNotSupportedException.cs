using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.Installation
{
    /// <summary>
    /// Thrown when a feature is required during package installation but could not be found in the current host.
    /// </summary>
    [Serializable]
    public class RequiredFeatureNotSupportedException : Exception
    {
        /// <summary>
        /// The type of feature that was required but was not found in the host.
        /// </summary>
        /// <remarks>
        /// This value is not serialized when thrown across AppDomains!
        /// </remarks>
        public Type FeatureType { get; private set; }

        public RequiredFeatureNotSupportedException() : base(Strings.RequiredFeatureUnsupportedException_DefaultMessageWithoutFeature) { }
        public RequiredFeatureNotSupportedException(Type featureType) : base(String.Format(CultureInfo.CurrentCulture, Strings.RequiredFeatureUnsupportedException_DefaultMessageWithFeature, featureType.Name)) { FeatureType = featureType; }
        public RequiredFeatureNotSupportedException(string message, Type featureType) : base(message) { FeatureType = featureType; }
        public RequiredFeatureNotSupportedException(string message, Type featureType, Exception inner) : base(message, inner) { FeatureType = featureType; }
        protected RequiredFeatureNotSupportedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }

        public RequiredFeatureNotSupportedException(string message) : base(message) { }
        public RequiredFeatureNotSupportedException(string message, Exception inner) : base(message, inner) { }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
