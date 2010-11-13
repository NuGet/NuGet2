using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class EnumerableExtensionsTest {

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void SafeQueryableThrowsIfSourceIsNull() {
            // Arrange
            IEnumerable<int> source = null;

            // Act 
            ExceptionAssert.ThrowsArgNull(() => source.AsSafeQueryable(), "source");
        }

        [TestMethod]
        public void SafeQueryableReturnsOriginalIQueryableWhenNotRewritingQueries() {
            // Arrange
            IQueryable<int> source = Enumerable.Range(0, 4).AsQueryable();

            // Act 
            IQueryable<int> result = source.AsSafeQueryable(rewriteQuery: false);

            // Assert
            Assert.AreEqual(result, source);
        }

        [TestMethod]
        public void SafeQueryableWrapsIEnumerableWhenNotRewritingQueries() {
            // Arrange
            IEnumerable<int> source = Enumerable.Range(0, 4);

            // Act 
            IQueryable<int> result = source.AsSafeQueryable(rewriteQuery: false);

            // Assert
            Assert.AreEqual(result.GetType(), typeof(EnumerableQuery<int>));
        }

        [TestMethod]
        public void SafeQueryableReturnsSafeEnumerableQueryWhenRewriting() {
            // Arrange
            IEnumerable<int> source = Enumerable.Range(0, 4);

            // Act 
            IQueryable<int> result = source.AsSafeQueryable(rewriteQuery: true);

            // Assert
            Assert.AreEqual(result.GetType(), typeof(SafeEnumerableQuery<int>));
        }

        [TestMethod]
        public void IsRewritingRequiredReturnsFalseIfApplicationIsRunningInFullTrust() {
            // Arrange
            AppDomain appDomain = GetFullTrustDomain();
            Assembly gacedAssembly = GetGACedAssembly();

            // Act
            bool requiresRewrite = EnumerableExtensions.IsRewritingRequired(appDomain, gacedAssembly);

            // Assert
            Assert.IsFalse(requiresRewrite);
        }

        [TestMethod]
        public void IsRewritingRequiredReturnsFalseIfAssemblyIsNotGACed() {
            // Arrange
            AppDomain appDomain = GetPartialTrustDomain();
            Assembly binDeployedAssembly = GetBinDeployedAssembly();

            // Act
            bool requiresRewrite = EnumerableExtensions.IsRewritingRequired(appDomain, binDeployedAssembly);

            // Assert
            Assert.IsFalse(requiresRewrite);
        }

        [TestMethod]
        public void IsRewritingRequiredReturnsTrueIfAppDomainIsPartiallyTrustedAndAssemblyIsGACed() {
            // Arrange
            AppDomain appDomain = GetPartialTrustDomain();
            Assembly binDeployedAssembly = GetGACedAssembly();

            // Act
            bool requiresRewrite = EnumerableExtensions.IsRewritingRequired(appDomain, binDeployedAssembly);

            // Assert
            Assert.IsTrue(requiresRewrite);
        }


        private AppDomain GetFullTrustDomain() {
            AppDomainSetup setup = new AppDomainSetup { ApplicationBase = TestContext.TestRunDirectory };
            PermissionSet permissions = new PermissionSet(PermissionState.Unrestricted);
            AppDomain appDomain = AppDomain.CreateDomain("Full Trust AppDomain", null, setup, permissions);

            Debug.Assert(appDomain.IsFullyTrusted);

            return appDomain;
        }

        // Based on article "How to host partial trust sandbox"
        // http://blogs.rev-net.com/ddewinter/2009/05/22/how-to-host-a-partial-trust-sandbox/
        private AppDomain GetPartialTrustDomain() {
            AppDomainSetup setup = new AppDomainSetup { ApplicationBase = TestContext.TestRunDirectory };
            PermissionSet permissions = new PermissionSet(permSet: null);
            permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
            permissions.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess));
            AppDomain appDomain = AppDomain.CreateDomain("Partial Trust AppDomain", null, setup, permissions);

            Debug.Assert(!appDomain.IsFullyTrusted);

            return appDomain;
        }

        private Assembly GetBinDeployedAssembly() {
            Assembly assembly = this.GetType().Assembly;
            Debug.Assert(!assembly.GlobalAssemblyCache, "Unit tests should never be GACed");

            return assembly;
        }

        private static Assembly GetGACedAssembly() {
            Assembly assembly = typeof(String).Assembly;
            Debug.Assert(assembly.GlobalAssemblyCache, "mscorlib has to be GACed");

            return assembly;
        }
    }
}
