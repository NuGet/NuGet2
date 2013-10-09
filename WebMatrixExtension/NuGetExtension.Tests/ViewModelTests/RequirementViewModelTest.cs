using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NuGet.WebMatrix.Tests.ViewModelTests
{
    
    public class RequirementViewModelTest
    {
        [Fact]
        public void Windows7NamedVersion()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindows7");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, "Windows 7");
            Assert.Equal(RequirementViewModel.Type, null);
            Assert.Equal(RequirementViewModel.Architecture, null);
            Assert.Equal(RequirementViewModel.Version, new Version(6, 1, 0, 0));
            Assert.False(RequirementViewModel.OrGreater);
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x86"));
        }

        [Fact]
        public void Windows8NamedVersion()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindows8");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, "Windows 8");
            Assert.Equal(RequirementViewModel.Type, null);
            Assert.Equal(RequirementViewModel.Architecture, null);
            Assert.Equal(RequirementViewModel.Version, new Version(6, 2, 0, 0));
            Assert.False(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsXPNamedVersion()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsXP");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, "Windows XP");
            Assert.Equal(RequirementViewModel.Type, null);
            Assert.Equal(RequirementViewModel.Architecture, null);
            Assert.Equal(RequirementViewModel.Version, new Version(5, 1, 0, 0));
            Assert.False(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsVistaNamedVersion()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsVista");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, "Windows Vista");
            Assert.Equal(RequirementViewModel.Type, null);
            Assert.Equal(RequirementViewModel.Architecture, null);
            Assert.Equal(RequirementViewModel.Version, new Version(6, 0, 0, 0));
            Assert.False(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void Windows7NamedVersionOrGreater()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindows7+");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, "Windows 7");
            Assert.Equal(RequirementViewModel.Type, null);
            Assert.Equal(RequirementViewModel.Architecture, null);
            Assert.Equal(RequirementViewModel.Version, new Version(6, 1, 0, 0));
            Assert.True(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void Windows8NamedVersionOrGreater()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindows8+");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, "Windows 8");
            Assert.Equal(RequirementViewModel.Type, null);
            Assert.Equal(RequirementViewModel.Architecture, null);
            Assert.Equal(RequirementViewModel.Version, new Version(6, 2, 0, 0));
            Assert.True(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsXPNamedVersionOrGreater()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsXP+");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, "Windows XP");
            Assert.Equal(RequirementViewModel.Type, null);
            Assert.Equal(RequirementViewModel.Architecture, null);
            Assert.Equal(RequirementViewModel.Version, new Version(5, 1, 0, 0));
            Assert.True(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsVistaNamedVersionOrGreater()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsVista+");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, "Windows Vista");
            Assert.Equal(RequirementViewModel.Type, null);
            Assert.Equal(RequirementViewModel.Architecture, null);
            Assert.Equal(RequirementViewModel.Version, new Version(6, 0, 0, 0));
            Assert.True(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsNumericVersion6()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsVersion6");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, null);
            Assert.Equal(RequirementViewModel.Type, null);
            Assert.Equal(RequirementViewModel.Architecture, null);
            Assert.Equal(RequirementViewModel.Version, new Version(6, 0, 0, 0));
            Assert.False(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsNumericVersion61()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsVersion6.1");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, null);
            Assert.Equal(RequirementViewModel.Type, null);
            Assert.Equal(RequirementViewModel.Architecture, null);
            Assert.Equal(RequirementViewModel.Version, new Version(6, 1, 0, 0));
            Assert.False(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsNumericVersion6OrGreater()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsVersion6+");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, null);
            Assert.Equal(RequirementViewModel.Type, null);
            Assert.Equal(RequirementViewModel.Architecture, null);
            Assert.Equal(RequirementViewModel.Version, new Version(6, 0, 0, 0));
            Assert.True(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsNumericVersion61OrGreater()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsVersion6.1+");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, null);
            Assert.Equal(RequirementViewModel.Type, null);
            Assert.Equal(RequirementViewModel.Architecture, null);
            Assert.Equal(RequirementViewModel.Version, new Version(6, 1, 0, 0));
            Assert.True(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsServerNumericVersion()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsServerVersion6.2");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, null);
            Assert.Equal(RequirementViewModel.Type, "Server");
            Assert.Equal(RequirementViewModel.Architecture, null);
            Assert.Equal(RequirementViewModel.Version, new Version(6, 2, 0, 0));
            Assert.False(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsServerNamedVersion()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindows7Server");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, "Windows 7");
            Assert.Equal(RequirementViewModel.Type, "Server");
            Assert.Equal(RequirementViewModel.Architecture, null);
            Assert.Equal(RequirementViewModel.Version, new Version(6, 1, 0, 0));
            Assert.False(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsClientNamedVersion()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindows8Client");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, "Windows 8");
            Assert.Equal(RequirementViewModel.Type, "Client");
            Assert.Equal(RequirementViewModel.Architecture, null);
            Assert.Equal(RequirementViewModel.Version, new Version(6, 2, 0, 0));
            Assert.False(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsClient32NamedVersionWithServicePack()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindows8Clientx86sp3");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, "Windows 8");
            Assert.Equal(RequirementViewModel.Type, "Client");
            Assert.Equal(RequirementViewModel.Architecture, "x86");
            Assert.Equal(RequirementViewModel.Version, new Version(6, 2, 0, 0));
            Assert.Equal(RequirementViewModel.ServicePack, new Version(3, 0, 0, 0));
            Assert.False(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsServer64NamedVersionWithServicePack()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindows7Serverx64sp3.1");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, "Windows 7");
            Assert.Equal(RequirementViewModel.Type, "Server");
            Assert.Equal(RequirementViewModel.Architecture, "x64");
            Assert.Equal(RequirementViewModel.Version, new Version(6, 1, 0, 0));
            Assert.Equal(RequirementViewModel.ServicePack, new Version(3, 1, 0, 0));
            Assert.False(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsClient32NamedVersionWithServicePackOrGreater()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindows8Clientx86sp3+");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, "Windows 8");
            Assert.Equal(RequirementViewModel.Type, "Client");
            Assert.Equal(RequirementViewModel.Architecture, "x86");
            Assert.Equal(RequirementViewModel.Version, new Version(6, 2, 0, 0));
            Assert.Equal(RequirementViewModel.ServicePack, new Version(3, 0, 0, 0));
            Assert.True(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsServer64NamedVersionWithServicePackOrGreater()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindows7Serverx64sp3.1+");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, "Windows 7");
            Assert.Equal(RequirementViewModel.Type, "Server");
            Assert.Equal(RequirementViewModel.Architecture, "x64");
            Assert.Equal(RequirementViewModel.Version, new Version(6, 1, 0, 0));
            Assert.Equal(RequirementViewModel.ServicePack, new Version(3, 1, 0, 0));
            Assert.True(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsClient32NumericVersionWithServicePack()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsClientx86Version6.2sp3");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, null);
            Assert.Equal(RequirementViewModel.Type, "Client");
            Assert.Equal(RequirementViewModel.Architecture, "x86");
            Assert.Equal(RequirementViewModel.Version, new Version(6, 2, 0, 0));
            Assert.Equal(RequirementViewModel.ServicePack, new Version(3, 0, 0, 0));
            Assert.False(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsServer64NumericVersionWithServicePack()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsServerx64Version6.1sp3.1");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, null);
            Assert.Equal(RequirementViewModel.Type, "Server");
            Assert.Equal(RequirementViewModel.Architecture, "x64");
            Assert.Equal(RequirementViewModel.Version, new Version(6, 1, 0, 0));
            Assert.Equal(RequirementViewModel.ServicePack, new Version(3, 1, 0, 0));
            Assert.False(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsClient32NumericVersionWithServicePackOrGreater()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsClientx86Version6.2sp3+");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, null);
            Assert.Equal(RequirementViewModel.Type, "Client");
            Assert.Equal(RequirementViewModel.Architecture, "x86");
            Assert.Equal(RequirementViewModel.Version, new Version(6, 2, 0, 0));
            Assert.Equal(RequirementViewModel.ServicePack, new Version(3, 0, 0, 0));
            Assert.True(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsServer64NumericVersionWithServicePackOrGreater()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsServerx64Version6.1sp3.1+");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, null);
            Assert.Equal(RequirementViewModel.Type, "Server");
            Assert.Equal(RequirementViewModel.Architecture, "x64");
            Assert.Equal(RequirementViewModel.Version, new Version(6, 1, 0, 0));
            Assert.Equal(RequirementViewModel.ServicePack, new Version(3, 1, 0, 0));
            Assert.True(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsNamedVersionWithArchitecture()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindows8x86");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, "Windows 8");
            Assert.Equal(RequirementViewModel.Type, null);
            Assert.Equal(RequirementViewModel.Architecture, "x86");
            Assert.Equal(RequirementViewModel.Version, new Version(6, 2, 0, 0));
            Assert.Equal(RequirementViewModel.ServicePack, null);
            Assert.False(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsNumericVersionWithArchitecture()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsx64Version6.2");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.Equal(RequirementViewModel.Product, "Windows");
            Assert.Equal(RequirementViewModel.NamedVersion, null);
            Assert.Equal(RequirementViewModel.Type, null);
            Assert.Equal(RequirementViewModel.Architecture, "x64");
            Assert.Equal(RequirementViewModel.Version, new Version(6, 2, 0, 0));
            Assert.Equal(RequirementViewModel.ServicePack, null);
            Assert.False(RequirementViewModel.OrGreater);
        }

        [Fact]
        public void WindowsNamedVersionRequirementIsMet()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindows7");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
        }

        [Fact]
        public void WindowsNamedVersionOrGreaterRequirementIsMet()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindows7+");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
        }

        [Fact]
        public void WindowsNumericVersionRequirementIsMet()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsVersion6.1");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
        }

        [Fact]
        public void WindowsNumericVersionOrGreaterRequirementIsMet()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsVersion6.1+");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
        }

        [Fact]
        public void WindowsNamedVersionSPRequirementIsMet()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindows7sp2");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(2, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(3, 1, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(3, 1, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(2, 1, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(2, 0, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(3, 1, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(3, 1, 0, 0), "Server", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(2, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(1, 1, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(1, 1, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
        }

        [Fact]
        public void WindowsNamedVersionSPOrGreaterRequirementIsMet()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindows7sp2+");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(2, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(3, 1, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(3, 1, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(2, 1, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(2, 0, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(3, 1, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(3, 1, 0, 0), "Server", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(2, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(1, 1, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(1, 1, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
        }

        [Fact]
        public void WindowsNumericVersionSPRequirementIsMet()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsVersion6.1sp2");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(2, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(3, 1, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(3, 1, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(2, 1, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(2, 0, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(3, 1, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(3, 1, 0, 0), "Server", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(2, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(1, 1, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(1, 1, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
        }

        [Fact]
        public void WindowsNumericVersionSPOrGreaterRequirementIsMet()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsVersion6.1sp2+");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(2, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(3, 1, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(3, 1, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(2, 1, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(2, 0, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(3, 1, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(3, 1, 0, 0), "Server", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(2, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(1, 1, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(1, 1, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
        }

        [Fact]
        public void WindowsNumericVersionSPTypeRequirementIsMet()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsServerVersion6.1sp2");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(2, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(3, 1, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(3, 1, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(2, 1, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(2, 0, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(3, 1, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(3, 1, 0, 0), "Server", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(2, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(1, 1, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(1, 1, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
        }

        [Fact]
        public void WindowsNumericVersionSPTypeOrGreaterRequirementIsMet()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsClientVersion6.1sp2+");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(2, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(3, 1, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(3, 1, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(2, 1, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(2, 0, 0, 0), "Server", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(3, 1, 0, 0), "Server", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(3, 1, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(2, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(1, 1, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(1, 1, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
        }

        [Fact]
        public void WindowsNumericVersionSPTypeArchRequirementIsMet()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsServerx64Version6.1sp2");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(2, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(3, 1, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(3, 1, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(2, 1, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(2, 0, 0, 0), "Server", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(3, 1, 0, 0), "Server", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(3, 1, 0, 0), "Server", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(2, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(1, 1, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(1, 1, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
        }

        [Fact]
        public void WindowsNumericVersionSPTypeArchOrGreaterRequirementIsMet()
        {
            RequirementViewModel RequirementViewModel = new RequirementViewModel("RequiresWindowsClientx86Version6.1sp2+");
            bool success = RequirementViewModel.Parse();
            Assert.True(success);
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(2, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(3, 1, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(3, 1, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(2, 1, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(2, 0, 0, 0), "Server", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(3, 1, 0, 0), "Server", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(3, 1, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(2, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(1, 1, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 0, 0), new Version(1, 1, 0, 0), "Client", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 1, 1, 1), new Version(0, 0, 0, 0), "Client", "x64"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.True(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Client", "x86"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 2, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(0, 0, 0, 0), "Server", "x64"));
            Assert.False(RequirementViewModel.IsMet(new Version(6, 0, 0, 0), new Version(1, 0, 0, 0), "Server", "x64"));
        }
    }
}
