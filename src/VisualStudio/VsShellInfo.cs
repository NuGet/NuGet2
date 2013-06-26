using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IVsShellInfo))]
    public class VsShellInfo : IVsShellInfo
    {
        private const int ExpressSku = 500;
        private const int WindowsSubSku = 0x4000;

        private readonly Lazy<bool> _isVisualStudioExpressForWindows8Thunk = new Lazy<bool>(ShellIsVisualStudioExpressForWindows8);

        public bool IsVisualStudioExpressForWindows8
        {
            get { return _isVisualStudioExpressForWindows8Thunk.Value; }
        }

        private static bool ShellIsVisualStudioExpressForWindows8()
        {
            if (VsVersionHelper.VsMajorVersion < 11)
            {
                // We only care for the Dev11 or above version of the express SKU.
                return false;
            }

            var vsShell = ServiceLocator.GetGlobalService<SVsShell, IVsShell>();

            if (vsShell == null)
            {
                return false;
            }

            object skuValue;
            var isExpress = ErrorHandler.Succeeded(vsShell.GetProperty((int)__VSSPROPID2.VSSPROPID_SKUEdition, out skuValue)) && skuValue is int && (int)skuValue == ExpressSku;
            if (!isExpress)
            {
                return false;
            }

            object subSkuValue;
            var isWindows = ErrorHandler.Succeeded(vsShell.GetProperty((int)__VSSPROPID2.VSSPROPID_SubSKUEdition, out subSkuValue)) && subSkuValue is int && (int)subSkuValue == WindowsSubSku;
            if (!isWindows)
            {
                return false;
            }

            return true;
        }
    }
}
