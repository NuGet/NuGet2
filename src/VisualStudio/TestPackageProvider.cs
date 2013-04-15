using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.VisualStudio
{
    [Export(typeof(IPackageProvider))]
    public class TestPackageProvider : IPackageProvider
    {
        public TestPackageProvider()
        {
        }

        public string Name
        {
            get { return "ASP.NET"; }
        }

        public string Description
        {
            get { return "Add/remove packages for your asp.net project"; }
        }

        public string Publisher
        {
            get { return "Microsoft Corporation"; }
        }

        public Uri PublisherUrl
        {
            get { return new Uri("http://asp.net"); }
        }

        public Uri IconUrl
        {
            get { return new Uri("http://www.gravatar.com/avatar/46c8189d84092927e2a78b63c37e7734?s=32&r=g&d=retro"); }
        }

        public void Invoke()
        {
            MessageHelper.ShowInfoMessage("Hello from One ASP.NET", "ASP.NET");
        }
    }

    [Export(typeof(IPackageProvider))]
    public class TestPackageProvider2 : IPackageProvider
    {
        public string Name
        {
            get { return "Windows Azure"; }
        }

        public string Description
        {
            get { return "Add/remove packages for your Windows Azure project"; }
        }

        public string Publisher
        {
            get { return "Microsoft Corporation"; }
        }

        public Uri PublisherUrl
        {
            get { return new Uri("http://windowsazure.net"); }
        }

        public Uri IconUrl
        {
            get { return new Uri("http://www.gravatar.com/avatar/425be63bdaaeeffd26d0172ed2030198?s=32&r=g&d=retro"); }
        }

        public void Invoke()
        {
            MessageHelper.ShowInfoMessage("Hello from Windows Azure", "Windows Azure");
        }
    }
}
