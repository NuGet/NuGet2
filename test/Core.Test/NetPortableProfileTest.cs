using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class NetPortableProfileTest
    {
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
            var profile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });

            var fw1 = new FrameworkName(".NETFramework, Version=4.0");
            var fw2 = new FrameworkName(".NETFramework, Version=4.5");
            var fw3 = new FrameworkName("Silverlight, Version=3.0");
            var fw4 = new FrameworkName("Silverlight, Version=5.0");
            var fw5 = new FrameworkName("WindowsPhone, Version=8.0");
            var fw6 = new FrameworkName("WindowsPhone, Version=7.0");
            var fw7 = new FrameworkName(".NETCore, Version=4.5");

            // Act & Assert
            Assert.False(profile.IsCompatibleWith(fw1));
            Assert.True(profile.IsCompatibleWith(fw2));
            Assert.False(profile.IsCompatibleWith(fw3));
            Assert.True(profile.IsCompatibleWith(fw4));
            Assert.True(profile.IsCompatibleWith(fw5));
            Assert.False(profile.IsCompatibleWith(fw6));
            Assert.False(profile.IsCompatibleWith(fw7));
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile1()
        {
            // Arrange 
            var profile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });

            var targetProfile = new NetPortableProfile(
                "MyProfile",
                new[] {
                           new FrameworkName(".NETFramework, Version=5.0"), 
                           new FrameworkName("Silverlight, Version=4.0")
                      });


            // Act & Assert
            Assert.True(profile.IsCompatibleWith(targetProfile));
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile2()
        {
            // Arrange 
            var profile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });

            var targetProfile = new NetPortableProfile(
                "MyProfile",
                new[] {
                           new FrameworkName(".NETFramework, Version=4.0"),
                           new FrameworkName("Silverlight, Version=4.0")
                      });

            // Act & Assert
            Assert.False(profile.IsCompatibleWith(targetProfile));
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile3()
        {
            // Arrange 
            var profile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });

            var targetProfile = new NetPortableProfile(
                "MyProfile",
                new[] {
                           new FrameworkName(".NETFramework, Version=4.5"),
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName(".NETCore, Version=4.0")
                      });

            // Act & Assert
            Assert.False(profile.IsCompatibleWith(targetProfile));
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile4()
        {
            // Arrange 
            var profile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName("Silverlight, Version=4.0"), 
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });

            var targetProfile = new NetPortableProfile(
                "MyProfile",
                new[] {
                           new FrameworkName(".NETCore, Version=4.0")
                      });

            // Act & Assert
            Assert.False(profile.IsCompatibleWith(targetProfile));
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile5()
        {
            // Arrange 
            var profile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.5"), 
                      });

            var targetProfile = new NetPortableProfile(
                "MyProfile",
                new[] {
                           new FrameworkName(".NETFramework, Version=4.5"), 
                           new FrameworkName(".NETCore, Version=4.0")
                      });

            // Act & Assert
            Assert.False(profile.IsCompatibleWith(targetProfile));
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile6()
        {
            // Arrange 
            var profile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName("WindowsPhone, Version=8.0"), 
                      });

            var targetProfile = new NetPortableProfile(
                "MyProfile",
                new[] {
                           new FrameworkName("WindowsPhone, Version=8.0"), 
                      });

            // Act & Assert
            Assert.True(profile.IsCompatibleWith(targetProfile));
        }

        [Fact]
        public void TestIsCompatibleWithPortableProfile7()
        {
            // Arrange 
            var profile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName("WindowsPhone, Version=7.0"), 
                      });

            var targetProfile = new NetPortableProfile(
                "MyProfile",
                new[] {
                           new FrameworkName("WindowsPhone, Version=7.1"), 
                      });

            // Act & Assert
            Assert.True(profile.IsCompatibleWith(targetProfile));
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

            // Act & Assert
            Assert.True(packageProfile.IsCompatibleWith(projectProfile));
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

            NetPortableProfileCollection profileCollection = new NetPortableProfileCollection();
            profileCollection.Add(profile1);

            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.0"), 
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName("WindowsPhone, Version=7.0"),
                           new FrameworkName("Windows, Version=8.0"),
                      });

            var projectProfile = new FrameworkName("MonoAndroid, Version=1.0");

            NetPortableProfileTable.Profiles = profileCollection;

            // Act & Assert
            Assert.True(packageProfile.IsCompatibleWith(projectProfile));
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

            NetPortableProfileCollection profileCollection = new NetPortableProfileCollection();
            profileCollection.Add(profile1);

            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.0"), 
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName("WindowsPhone, Version=7.0"),
                           new FrameworkName("Windows, Version=8.0"),
                      });

            var projectProfile = new FrameworkName("MonoAndroid, Version=2.0");

            NetPortableProfileTable.Profiles = profileCollection;

            // Act & Assert
            Assert.True(packageProfile.IsCompatibleWith(projectProfile));
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

            NetPortableProfileCollection profileCollection = new NetPortableProfileCollection();
            profileCollection.Add(profile1);

            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.0"), 
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName("WindowsPhone, Version=7.0"),
                           new FrameworkName("Windows, Version=8.0"),
                      });

            var projectProfile = new FrameworkName("MonoAndroid, Version=1.0");

            NetPortableProfileTable.Profiles = profileCollection;

            // Act & Assert
            Assert.False(packageProfile.IsCompatibleWith(projectProfile));
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

            NetPortableProfileCollection profileCollection = new NetPortableProfileCollection();
            profileCollection.Add(profile1);
            profileCollection.Add(profile2);
            profileCollection.Add(profile3);

            var packageProfile = new NetPortableProfile(
                "ProfileXXX",
                new[] { 
                           new FrameworkName(".NETFramework, Version=4.0"), 
                           new FrameworkName("Silverlight, Version=4.0"),
                           new FrameworkName("WindowsPhone, Version=7.0"),
                           new FrameworkName("Windows, Version=8.0"),
                      });

            var projectProfile = new FrameworkName("MonoAndroid, Version=2.0");

            NetPortableProfileTable.Profiles = profileCollection;

            // Act & Assert
            Assert.True(packageProfile.IsCompatibleWith(projectProfile));
        }

        [Fact]
        public void TestParseWithCustomProfileString1()
        {
            // Arrange & Act
            var profile = NetPortableProfile.Parse("sl3+net+netcore45");

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
            var profile = NetPortableProfile.Parse("wp7");

            // Assert
            Assert.Equal(1, profile.SupportedFrameworks.Count);
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("WindowsPhone, Version=7.0")));
        }

        [Fact]
        public void TestParseWithInvalidCustomProfileReturnsNull()
        {
            // Arrange & Act
            var profile = NetPortableProfile.Parse("Profile3284");

            // Assert
            Assert.Null(profile);
        }

        [Fact]
        public void TestParseWithCustomProfileString3()
        {
            // Arrange & Act
            var profile = NetPortableProfile.Parse("wp71+win8+monoandroid1.6+monotouch1.0+sl4+net45");

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
            // Arrange & Act
            var profileCollection = new NetPortableProfileCollection();
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
            profileCollection.Add(profile1);
            profileCollection.Add(profile2);

            NetPortableProfileTable.Profiles = profileCollection;

            var profile = NetPortableProfile.Parse("Profile2");

            // Assert
            Assert.Equal(3, profile.SupportedFrameworks.Count);
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName(".NetCore, Version=4.5")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("Silverlight, Version=3.0")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("WindowsPhone, Version=7.1")));
        }

        [Fact]
        public void TestParseWithCustomProfileString4WithDifferentOrderingOfFrameworks()
        {
            // Arrange & Act
            var profileCollection = new NetPortableProfileCollection();
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
            profileCollection.Add(profile1);
            profileCollection.Add(profile2);

            NetPortableProfileTable.Profiles = profileCollection;

            var profile = NetPortableProfile.Parse("net45+sl40+MonoTouch+wp71+MonoAndroid20");

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
            // Arrange & Act
            var profileCollection = new NetPortableProfileCollection();
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
            profileCollection.Add(profile1);
            profileCollection.Add(profile2);

            NetPortableProfileTable.Profiles = profileCollection;

            // Default value of second parameter treatOptionalFrameworksAsSupportedFrameworks is false
            var profile = NetPortableProfile.Parse("net45+sl40+wp71+MonoTouch+MonoAndroid20");

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
            // Arrange & Act
            var profileCollection = new NetPortableProfileCollection();
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
            profileCollection.Add(profile1);
            profileCollection.Add(profile2);

            NetPortableProfileTable.Profiles = profileCollection;

            var profile = NetPortableProfile.Parse("net45+sl40+wp71+MonoTouch+MonoAndroid20", treatOptionalFrameworksAsSupportedFrameworks: true);

            // Assert
            Assert.Equal(5, profile.SupportedFrameworks.Count);
            Assert.Equal(0, profile.OptionalFrameworks.Count);
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName(".NETFramework, Version=4.5")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("Silverlight, Version=4.0")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("WindowsPhone, Version=7.1")));
            Assert.True(profile.SupportedFrameworks.Contains(new FrameworkName("MonoTouch, Version=0.0")));
        }
    }
}