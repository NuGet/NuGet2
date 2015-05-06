using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class NullSettingsTest
    {
        public static IEnumerable<object[]> WriteOperationsData
        {
            get
            {
                var settings = NullSettings.Instance;
                yield return new object[] { (Assert.ThrowsDelegate)(() => settings.SetValue("section", "key", "value")), "SetValue" };
                yield return new object[] { (Assert.ThrowsDelegate)(() => settings.SetValues("section", new[] { new SettingValue("key", "value", false) })), "SetValues" };
                yield return new object[] { (Assert.ThrowsDelegate)(() => settings.SetNestedValues("section", "key", new[] { new KeyValuePair<string, string>("key1", "value1") })), "SetNestedValues" };
                yield return new object[] { (Assert.ThrowsDelegate)(() => settings.DeleteSection("section")), "DeleteSection" };
                yield return new object[] { (Assert.ThrowsDelegate)(() => settings.DeleteValue("section", "key")), "DeleteValue" };
            }
        }

        [Theory]
        [PropertyData("WriteOperationsData")]
        public void NullSettingsThrowsIfWriteOperationMethodsAreInvoked(Assert.ThrowsDelegate throwsDelegate, string methodName)
        {
            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(throwsDelegate,
                String.Format("\"{0}\" cannot be called on a NullSettings. This may be caused on account of insufficient permissions to read or write to \"%AppData%\\NuGet\\NuGet.config\".", methodName));
        }
    }
}
