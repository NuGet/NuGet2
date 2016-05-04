using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class NetPortableProfileTableTest
    {
        [Fact]
        public async Task NetPortableProfileTable_LoadTestForThreadSafety()
        {
            // Arrange
            var tasks = Enumerable
                .Range(1, 20)
                .Select(task => Task.Run(async () =>
                {
                    await Task.Yield();

                    for (int iteration = 0; iteration < 50; iteration++)
                    {
                        NetPortableProfileTable.SetProfileCollection(null);

                        // Act
                        var result = NetPortableProfileTable.GetProfile("not-real");

                        // Assert
                        Assert.Null(result);
                    }
                }));

            await Task.WhenAll(tasks);
        }

        [Fact]
        public void LoadSupportedFrameworkCorrectsTheWP7Framework()
        {
            // Arrange
            string content = @"
<Framework
    Identifier=""Silverlight""
    Profile=""WindowsPhone*""
    MinimumVersion=""4.0""
    DisplayName=""Windows Phone""
    MinimumVersionDisplayName=""7"" />";

            // Act
            FrameworkName fx = NetPortableProfileTable.LoadSupportedFramework(content.AsStream());

            // Assert
            Assert.Equal(new FrameworkName("Silverlight, Version=3.0, Profile=WindowsPhone"), fx);

        }

        [Fact]
        public void LoadSupportedFrameworkCorrectsTheWP71Framework()
        {
            // Arrange
            string content = @"
<Framework
    Identifier=""Silverlight""
    Profile=""WindowsPhone7*""
    MinimumVersion=""4.0""
    DisplayName=""Windows Phone""
    MinimumVersionDisplayName=""7.5"" />";

            // Act
            FrameworkName fx = NetPortableProfileTable.LoadSupportedFramework(content.AsStream());

            // Assert
            Assert.Equal(new FrameworkName("Silverlight, Version=4.0, Profile=WindowsPhone71"), fx);
        }

        [Fact]
        public void LoadPortableProfileWithMonoAsSupportedFramework()
        {
            // Arrange
            string content1 = @"
<Framework
    Identifier="".NETFramework""
    Profile=""*""
    MinimumVersion=""4.5""
    DisplayName="".NET Framework"" />";

            string content2 = @"
<Framework
    Identifier=""MonoTouch""
    MinimumVersion=""1.0""
    DisplayName=""Mono Touch"" />";

            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile("frameworkFile1.xml", content1);
            mockFileSystem.AddFile("frameworkFile2.xml", content2);

            var frameworkFiles = new string[] { "frameworkFile1.xml", "frameworkFile2.xml" };

            // Act
            var netPortableProfile = NetPortableProfileTable.LoadPortableProfile("4.5.0.0", "Profile1", mockFileSystem, frameworkFiles);

            // Assert
            Assert.True(netPortableProfile.SupportedFrameworks.Count == 1);
            Assert.True(netPortableProfile.SupportedFrameworks.Contains(new FrameworkName(".NETFramework, Version=4.5")));

            Assert.True(netPortableProfile.OptionalFrameworks.Count == 1);
            Assert.True(netPortableProfile.OptionalFrameworks.Contains(new FrameworkName("MonoTouch, Version=1.0")));
        }

        [Fact]
        public void LoadPortableProfileWithXamarinAsSupportedFramework()
        {
            // Arrange
            string content1 = @"
<Framework
    Identifier="".NETFramework""
    Profile=""*""
    MinimumVersion=""4.5""
    DisplayName="".NET Framework"" />";

            string content2 = @"
<Framework
    Identifier=""Xamarin.Mac""
    MinimumVersion=""1.0""
    DisplayName=""Xamarin.Mac"" />";

            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile("frameworkFile1.xml", content1);
            mockFileSystem.AddFile("frameworkFile2.xml", content2);

            var frameworkFiles = new string[] { "frameworkFile1.xml", "frameworkFile2.xml" };

            // Act
            var netPortableProfile = NetPortableProfileTable.LoadPortableProfile("4.5.0.0", "Profile1", mockFileSystem, frameworkFiles);

            // Assert
            Assert.True(netPortableProfile.SupportedFrameworks.Count == 1);
            Assert.True(netPortableProfile.SupportedFrameworks.Contains(new FrameworkName(".NETFramework, Version=4.5")));

            Assert.True(netPortableProfile.OptionalFrameworks.Count == 1);
            Assert.True(netPortableProfile.OptionalFrameworks.Contains(new FrameworkName("Xamarin.Mac, Version=1.0")));
        }

        [Fact]
        public void LoadPortableProfileWithXamarinWatchOSAsSupportedFramework()
        {
            // Arrange
            string content1 = @"
<Framework
    Identifier="".NETFramework""
    Profile=""*""
    MinimumVersion=""4.5""
    DisplayName="".NET Framework"" />";

            string content2 = @"
<Framework
    Identifier=""Xamarin.WatchOS""
    MinimumVersion=""1.0""
    DisplayName=""Xamarin.WatchOS"" />";

            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile("frameworkFile1.xml", content1);
            mockFileSystem.AddFile("frameworkFile2.xml", content2);

            var frameworkFiles = new string[] { "frameworkFile1.xml", "frameworkFile2.xml" };

            // Act
            var netPortableProfile = NetPortableProfileTable.LoadPortableProfile("4.5.0.0", "Profile1", mockFileSystem, frameworkFiles);

            // Assert
            Assert.True(netPortableProfile.SupportedFrameworks.Count == 1);
            Assert.True(netPortableProfile.SupportedFrameworks.Contains(new FrameworkName(".NETFramework, Version=4.5")));

            Assert.True(netPortableProfile.OptionalFrameworks.Count == 1);
            Assert.True(netPortableProfile.OptionalFrameworks.Contains(new FrameworkName("Xamarin.WatchOS, Version=1.0")));
        }

        [Fact]
        public void LoadPortableProfileWithXamarinTVOSAsSupportedFramework()
        {
            // Arrange
            string content1 = @"
<Framework
    Identifier="".NETFramework""
    Profile=""*""
    MinimumVersion=""4.5""
    DisplayName="".NET Framework"" />";

            string content2 = @"
<Framework
    Identifier=""Xamarin.TVOS""
    MinimumVersion=""1.0""
    DisplayName=""Xamarin.TVOS"" />";

            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile("frameworkFile1.xml", content1);
            mockFileSystem.AddFile("frameworkFile2.xml", content2);

            var frameworkFiles = new string[] { "frameworkFile1.xml", "frameworkFile2.xml" };

            // Act
            var netPortableProfile = NetPortableProfileTable.LoadPortableProfile("4.5.0.0", "Profile1", mockFileSystem, frameworkFiles);

            // Assert
            Assert.True(netPortableProfile.SupportedFrameworks.Count == 1);
            Assert.True(netPortableProfile.SupportedFrameworks.Contains(new FrameworkName(".NETFramework, Version=4.5")));

            Assert.True(netPortableProfile.OptionalFrameworks.Count == 1);
            Assert.True(netPortableProfile.OptionalFrameworks.Contains(new FrameworkName("Xamarin.TVOS, Version=1.0")));
        }
    }
}
