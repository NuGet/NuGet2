using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuPack.Test {
    [TestClass]
    public class PackageRepositoryFactoryTest {
        [TestMethod]
        public void CreateRepositoryThrowsIfNullOrEmpty() {
            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => PackageRepositoryFactory.Default.CreateRepository(null), "source");
        }
    }
}
