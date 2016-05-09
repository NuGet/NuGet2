using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;
using Xunit;

namespace NuGet.Test
{
    public class NetPortableProfileTest
    {
        [Fact]
        public void IsCompatible_SupportsCustomProfileNumber()
        {
            // Arrange
            var profile = new NetPortableProfile(
               "Profile500",
               new[]  {
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName("WindowsPhone, Version=7.1"),
                      });

            var tc = new TestContext(profile);
            var project = new FrameworkName("Silverlight, Version=5.0");
            var compatible = new FrameworkName(".NETPortable, Version=4.0, Profile=Profile500");
            var notCompatible = new FrameworkName(".NETPortable, Version=4.0, Profile=Profile501");

            // Act & Assert
            tc.VerifyIsCompatible(project, compatible, true);
            tc.VerifyIsCompatible(project, notCompatible, false);
        }

        [Fact]
        public void IsCompatible_SupportsCustomProfileName()
        {
            // Arrange
            var profile = new NetPortableProfile(
                "MyProfile",
               new[]  {
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName("WindowsPhone, Version=7.1"),
                      });

            var tc = new TestContext(profile);
            var project = new FrameworkName("Silverlight, Version=5.0");
            var compatible = new FrameworkName(".NETPortable, Version=4.0, Profile=MyProfile");
            var notCompatible = new FrameworkName(".NETPortable, Version=4.0, Profile=YourProfile");

            // Act & Assert
            tc.VerifyIsCompatible(project, compatible, true);
            tc.VerifyIsCompatible(project, notCompatible, false);
        }

        [Fact]
        public void NamePropertyReturnsCorrectValue()
        {
            // Arrange 
            var profile = new NetPortableProfile("ProfileXXX", new [] { new FrameworkName(".NETFramework, Version=4.5") });

            // Act
            string name = profile.Name;

            // Assert
            Assert.Equal("ProfileXXX", name);
        }

        [Fact]
        public void SupportedFrameworksReturnsCorrectValue()
        {
            // Arrange 
            var profile = new NetPortableProfile(
                "ProfileXXX",
                new[] { new FrameworkName(".NETFramework, Version=4.5"), new FrameworkName("Silverlight, Version=4.0") });

            // Act
            ISet<FrameworkName> frameworks = profile.SupportedFrameworks;

            // Assert
            Assert.Equal(2, frameworks.Count);
            Assert.True(frameworks.Contains(new FrameworkName(".NETFramework, Version=4.5")));
            Assert.True(frameworks.Contains(new FrameworkName("Silverlight, Version=4.0")));
        }

        [Fact]
        public void TestIsCompatibleWithFrameworkName()
        {
            // Arrange 
            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });

            var tc = new TestContext(packageProfile);
            var packageFramework = GetFrameworkName(packageProfile);

            var fw1 = new FrameworkName(".NETFramework, Version=4.0");
            var fw2 = new FrameworkName(".NETFramework, Version=4.5");
            var fw3 = new FrameworkName("Silverlight, Version=3.0");
            var fw4 = new FrameworkName("Silverlight, Version=5.0");
            var fw5 = new FrameworkName("WindowsPhone, Version=8.0");
            var fw6 = new FrameworkName("WindowsPhone, Version=7.0");
            var fw7 = new FrameworkName(".NETCore, Version=4.5");

            // Act & Assert
            tc.VerifyIsCompatible(fw1, packageFramework, false);
            tc.VerifyIsCompatible(fw2, packageFramework, true);
            tc.VerifyIsCompatible(fw3, packageFramework, false);
            tc.VerifyIsCompatible(fw4, packageFramework, true);
            tc.VerifyIsCompatible(fw5, packageFramework, true);
            tc.VerifyIsCompatible(fw6, packageFramework, false);
            tc.VerifyIsCompatible(fw7, packageFramework, false);
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile1()
        {
            // Arrange 
            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });

            var projectProfile = new NetPortableProfile(
                "MyProfile",
                new[] {
                           new FrameworkName(".NETFramework, Version=5.0"), 
                           new FrameworkName("Silverlight, Version=4.0")
                      });

            var tc = new TestContext(packageProfile, projectProfile);
            var packageFramework = GetFrameworkName(packageProfile);
            var projectFramework = GetFrameworkName(projectProfile);

            // Act & Assert
            tc.VerifyIsCompatible(projectFramework, packageFramework, true);
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile2()
        {
            // Arrange 
            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });

            var projectProfile = new NetPortableProfile(
                "MyProfile",
                new[] {
                           new FrameworkName(".NETFramework, Version=4.0"),
                           new FrameworkName("Silverlight, Version=4.0")
                      });

            var tc = new TestContext(packageProfile, projectProfile);

            var packageFramework = GetFrameworkName(packageProfile);
            var projectFramework = GetFrameworkName(projectProfile);

            // Act & Assert
            tc.VerifyIsCompatible(projectFramework, packageFramework, false);
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile3()
        {
            // Arrange 
            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });

            var projectProfile = new NetPortableProfile(
                "MyProfile",
                new[] {
                           new FrameworkName(".NETFramework, Version=4.5"),
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName(".NETCore, Version=4.0")
                      });

            var tc = new TestContext(packageProfile, projectProfile);

            var packageFramework = GetFrameworkName(packageProfile);
            var projectFramework = GetFrameworkName(projectProfile);

            // Act & Assert
            tc.VerifyIsCompatible(projectFramework, packageFramework, false);
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile4()
        {
            // Arrange 
            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });

            var projectProfile = new NetPortableProfile(
                "MyProfile",
                new[] {
                           new FrameworkName(".NETCore, Version=4.0")
                      });

            var tc = new TestContext(packageProfile, projectProfile);

            var packageFramework = GetFrameworkName(packageProfile);
            var projectFramework = GetFrameworkName(projectProfile);

            // Act & Assert
            tc.VerifyIsCompatible(projectFramework, packageFramework, false);
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile5()
        {
            // Arrange 
            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                      });

            var projectProfile = new NetPortableProfile(
                "MyProfile",
                new[] {
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName(".NETCore, Version=4.0")
                      });

            var tc = new TestContext(packageProfile, projectProfile);

            var packageFramework = GetFrameworkName(packageProfile);
            var projectFramework = GetFrameworkName(projectProfile);

            // Act & Assert
            tc.VerifyIsCompatible(projectFramework, packageFramework, false);
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile6()
        {
            // Arrange 
            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName("WindowsPhone, Version=8.0"), 
                      });

            var projectProfile = new NetPortableProfile(
                "MyProfile",
                new[] {
                           new FrameworkName("WindowsPhone, Version=8.0"), 
                      });

            var tc = new TestContext(packageProfile, projectProfile);

            var packageFramework = GetFrameworkName(packageProfile);
            var projectFramework = GetFrameworkName(projectProfile);

            // Act & Assert
            tc.VerifyIsCompatible(projectFramework, packageFramework, true);
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile7()
        {
            // Arrange 
            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName("WindowsPhone, Version=7.0"), 
                      });

            var projectProfile = new NetPortableProfile(
                "MyProfile",
                new[] {
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });

            var tc = new TestContext(packageProfile, projectProfile);

            var packageFramework = GetFrameworkName(packageProfile);
            var projectFramework = GetFrameworkName(projectProfile);

            // Act & Assert
            tc.VerifyIsCompatible(projectFramework, packageFramework, true);
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile8WithMono()
        {
            // Arrange 
            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.0"), 
                           new FrameworkName("Silverlight, Version=4.0"),                             
                      });

            var projectProfile = new NetPortableProfile(
               "MyProfile",
               new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=5.0"), 
                      },
               new[] { 
                           new FrameworkName("MonoTouch, Version=0.0"), 
                           new FrameworkName("MonoAndroid, Version=1.0"), 
                      });

            var tc = new TestContext(packageProfile, projectProfile);

            var packageFramework = GetFrameworkName(packageProfile);
            var projectFramework = GetFrameworkName(projectProfile);

            // Act & Assert
            tc.VerifyIsCompatible(projectFramework, packageFramework, true);
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile9WithMonoProjectWithVersionEqualToInstalledProfile()
        {
            // Arrange
            var profile1 = new NetPortableProfile(
               "Profile1",
               new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName("WindowsPhone, Version=7.1"),
                           new FrameworkName("Windows, Version=8.0"),
                      },
               new[] { 
                           new FrameworkName("MonoTouch, Version=1.0"), 
                           new FrameworkName("MonoAndroid, Version=1.0"),
                           new FrameworkName("MonoMac, Version=1.0"),
                      });

            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.0"), 
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName("WindowsPhone, Version=7.0"),
                           new FrameworkName("Windows, Version=8.0"),
                      });

            var tc = new TestContext(profile1, packageProfile);
            
            var packageFramework = GetFrameworkName(profile1);
            var projectFramework = new FrameworkName("MonoAndroid, Version=1.0");

            // Act & Assert
            tc.VerifyIsCompatible(projectFramework, packageFramework, true);
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile10WithMonoProjectWithVersionGreaterThanInstalledProfile()
        {
            // Arrange
            var profile1 = new NetPortableProfile(
               "Profile1",
               new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName("WindowsPhone, Version=7.1"),
                           new FrameworkName("Windows, Version=8.0"),
                      },
               new[] { 
                           new FrameworkName("MonoTouch, Version=1.0"), 
                           new FrameworkName("MonoAndroid, Version=1.0"),
                           new FrameworkName("MonoMac, Version=1.0"),
                      });
            
            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.0"), 
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName("WindowsPhone, Version=7.0"),
                           new FrameworkName("Windows, Version=8.0"),
                      });

            var tc = new TestContext(profile1, packageProfile);

            var packageFramework = GetFrameworkName(packageProfile);
            var projectFramework = new FrameworkName("MonoAndroid, Version=2.0");

            // Act & Assert
            tc.VerifyIsCompatible(projectFramework, packageFramework, true);
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile11WithMonoProjectWithVersionLesserThanInstalledProfile()
        {
            // Arrange
            var profile1 = new NetPortableProfile(
               "Profile1",
               new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName("WindowsPhone, Version=7.1"),
                           new FrameworkName("Windows, Version=8.0"),
                      },
               new[] { 
                           new FrameworkName("MonoTouch, Version=1.0"), 
                           new FrameworkName("MonoAndroid, Version=2.0"),
                           new FrameworkName("MonoMac, Version=1.0"),
                      });

            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.0"), 
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName("WindowsPhone, Version=7.0"),
                           new FrameworkName("Windows, Version=8.0"),
                      });

            var tc = new TestContext(profile1, packageProfile);

            var packageFramework = GetFrameworkName(packageProfile);
            var projectFramework = new FrameworkName("MonoAndroid, Version=1.0");

            // Act & Assert
            tc.VerifyIsCompatible(projectFramework, packageFramework, false);
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile12WithMonoProjectWithMultipleInstalledProfileHavingMonoOfDifferentVersions()
        {
            // Arrange
            var profile1 = new NetPortableProfile(
               "Profile1",
               new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName("WindowsPhone, Version=7.1"),
                           new FrameworkName("Windows, Version=8.0"),
                      },
               new[] { 
                           new FrameworkName("MonoTouch, Version=1.0"), 
                           new FrameworkName("MonoAndroid, Version=3.0"),
                           new FrameworkName("MonoMac, Version=1.0"),
                      });

            var profile2 = new NetPortableProfile(
               "Profile2",
               new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName("WindowsPhone, Version=7.1"),
                           new FrameworkName("Windows, Version=8.0"),
                      },
               new[] { 
                           new FrameworkName("MonoAndroid, Version=1.0"),
                           new FrameworkName("MonoMac, Version=1.0"),
                      });

            var profile3 = new NetPortableProfile(
               "Profile3",
               new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.5"),
                      },
               new[] { 
                           new FrameworkName("MonoAndroid, Version=3.0"),
                      });

            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.0"), 
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName("WindowsPhone, Version=7.0"),
                           new FrameworkName("Windows, Version=8.0"),
                      });

            var tc = new TestContext(profile1, profile2, profile3, packageProfile);

            var packageFramework = GetFrameworkName(packageProfile);
            var projectFramework = new FrameworkName("MonoAndroid, Version=2.0");

            // Act & Assert
            tc.VerifyIsCompatible(projectFramework, packageFramework, true);
        }

        [Fact]
        public void TestParseWithCustomProfileString1()
        {
            // Arrange & Act
            var profile = NetPortableProfile.Parse(NetPortableProfileTable.Instance, "sl3+net+netcore45");

            // Assert
            Assert.Equal(3, profile.SupportedFrameworks.Count);
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("Silverlight, Version=3.0")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName(".NETFramework, Version=0.0")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName(".NETCore, Version=4.5")));
        }

        [Fact]
        public void TestParseWithCustomProfileString2()
        {
            // Arrange & Act
            var profile = NetPortableProfile.Parse(NetPortableProfileTable.Instance, "wp7");

            // Assert
            Assert.Equal(1, profile.SupportedFrameworks.Count);
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("WindowsPhone, Version=7.0")));
        }

        [Fact]
        public void TestParseWithInvalidCustomProfileReturnsNull()
        {
            // Arrange & Act
            var profile = NetPortableProfile.Parse(NetPortableProfileTable.Instance, "Profile3284");

            // Assert
            Assert.Null(profile);
        }

        [Fact]
        public void TestParseWithCustomProfileString3()
        {
            // Arrange & Act
            var profile = NetPortableProfile.Parse(
                NetPortableProfileTable.Instance,
                "wp71+win8+monoandroid1.6+monotouch1.0+sl4+net45");

            // Assert
            Assert.Equal(6, profile.SupportedFrameworks.Count);
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("WindowsPhone, Version=7.1")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("Windows, Version=8.0")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("MonoAndroid, Version=1.6")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("MonoTouch, Version=1.0")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("Silverlight, Version=4.0")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName(".NETFramework, Version=4.5")));
        }

        [Fact]
        public void TestParseWithStandardProfileString()
        {
            // Arrange
            var collection = new NetPortableProfileCollection();
            var profile1 = new NetPortableProfile(
               "Profile1",
               new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });

            var profile2 = new NetPortableProfile(
               "Profile2",
               new[] { 
                           new FrameworkName(".NetCore, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=3.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });
            collection.Add(profile1);
            collection.Add(profile2);

            var table = new NetPortableProfileTable(collection);

            // Act
            var profile = NetPortableProfile.Parse(table, "Profile2");

            // Assert
            Assert.Equal(3, profile.SupportedFrameworks.Count);
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName(".NetCore, Version=4.5")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("Silverlight, Version=3.0")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("WindowsPhone, Version=7.1")));
        }

        [Fact]
        public void TestParseWithCustomProfileString4WithDifferentOrderingOfFrameworks()
        {
            // Arrange
            var collection = new NetPortableProfileCollection();
            var profile1 = new NetPortableProfile(
               "Profile1",
               new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      },
               new[] { 
                           new FrameworkName("MonoTouch, Version=0.0"), 
                           new FrameworkName("MonoAndroid, Version=2.0"), 
                      });

            var profile2 = new NetPortableProfile(
               "Profile2",
               new[] { 
                           new FrameworkName(".NetCore, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=3.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });
            collection.Add(profile1);
            collection.Add(profile2);

            var table = new NetPortableProfileTable(collection);

            // Act
            var profile = NetPortableProfile.Parse(
                table,
                "net45+sl40+MonoTouch+wp71+MonoAndroid20");

            // Assert
            Assert.Equal(5, profile.SupportedFrameworks.Count);
            Assert.Equal(0, profile.OptionalFrameworks.Count);
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName(".NETFramework, Version=4.5")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("Silverlight, Version=4.0")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("WindowsPhone, Version=7.1")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("MonoTouch, Version=0.0")));
        }    

        [Fact]
        public void TestParseWithCustomProfileString5WithOptionalFrameworkAndTreatedAsOptional()
        {
            // Arrange
            var collection = new NetPortableProfileCollection();
            var profile1 = new NetPortableProfile(
               "Profile1",
               new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      },
               new[] { 
                           new FrameworkName("MonoTouch, Version=0.0"), 
                           new FrameworkName("MonoAndroid, Version=2.0"), 
                      });

            var profile2 = new NetPortableProfile(
               "Profile2",
               new[] { 
                           new FrameworkName(".NetCore, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=3.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });
            collection.Add(profile1);
            collection.Add(profile2);

            var table = new NetPortableProfileTable(collection);

            // Act
            // Default value of second parameter treatOptionalFrameworksAsSupportedFrameworks is false
            var profile = NetPortableProfile.Parse(
                table,
                "net45+sl40+wp71+MonoTouch+MonoAndroid20");

            // Assert
            Assert.Equal(3, profile.SupportedFrameworks.Count);
            Assert.Equal(2, profile.OptionalFrameworks.Count);
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName(".NETFramework, Version=4.5")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("Silverlight, Version=4.0")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("WindowsPhone, Version=7.1")));
            Assert.True(profile.OptionalFrameworks.Contains(new FrameworkName("MonoTouch, Version=0.0")));
            Assert.True(profile.OptionalFrameworks.Contains(new FrameworkName("MonoAndroid, Version=2.0")));
        }

        [Fact]
        public void TestParseWithCustomProfileString6WithOptionalFrameworkAndTreatedAsSupported()
        {
            // Arrange
            var collection = new NetPortableProfileCollection();
            var profile1 = new NetPortableProfile(
               "Profile1",
               new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      },
               new[] { 
                           new FrameworkName("MonoTouch, Version=0.0"), 
                           new FrameworkName("MonoAndroid, Version=2.0"), 
                      });

            var profile2 = new NetPortableProfile(
               "Profile2",
               new[] { 
                           new FrameworkName(".NetCore, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=3.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });
            collection.Add(profile1);
            collection.Add(profile2);

            var table = new NetPortableProfileTable(collection);

            // Act
            var profile = NetPortableProfile.Parse(
                table,
                "net45+sl40+wp71+MonoTouch+MonoAndroid20",
                treatOptionalFrameworksAsSupportedFrameworks: true);

            // Assert
            Assert.Equal(5, profile.SupportedFrameworks.Count);
            Assert.Equal(0, profile.OptionalFrameworks.Count);
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName(".NETFramework, Version=4.5")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("Silverlight, Version=4.0")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("WindowsPhone, Version=7.1")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("MonoTouch, Version=0.0")));
        }

        private static FrameworkName GetFrameworkName(NetPortableProfile profile)
        {
            return new FrameworkName(
                ".NETPortable",
                new Version(profile.FrameworkVersion.Substring(1)), // "v0.0" becomes "0.0"
                profile.Name);
        }

        private class TestContext
        {
            public TestContext(params NetPortableProfile[] portableProfiles)
            {
                Collection = new NetPortableProfileCollection();
                Collection.AddRange(portableProfiles);

                Table = new NetPortableProfileTable(Collection);

                CompatibilityProvider = new ReferenceAssemblyCompatibilityProvider(Collection);

                NameProvider = new ReferenceAssemblyFrameworkNameProvider(Collection);
            }

            public NetPortableProfileCollection Collection { get; private set; }
            public ReferenceAssemblyCompatibilityProvider CompatibilityProvider { get; private set; }
            public ReferenceAssemblyFrameworkNameProvider NameProvider { get; private set; }
            public NetPortableProfileTable Table { get; private set; }

            public void VerifyIsCompatible(FrameworkName project, FrameworkName package, bool expected)
            {
                var actual = VersionUtility.IsCompatible(
                    Table,
                    CompatibilityProvider,
                    NameProvider,
                    project,
                    package);

                Assert.True(
                    actual == expected,
                    string.Format(
                        "'{0}' should {1}be compatible with '{2}'.",
                        project,
                        expected ? string.Empty : "not ",
                        package));
            }
        }
    }
}