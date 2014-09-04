using System;
using System.ComponentModel.Composition;

namespace NuGet.Client.V3Shim
{
    /// <summary>
    /// Ensures that there is only one instance of the shim controller.
    /// </summary>
    [Export(typeof(IShimControllerProvider))]
    public class ShimControllerProvider : IShimControllerProvider
    {
        private IShimController _controller;

        [Import]
        private IDebugConsoleController _debugLogger = null;

        public IShimController Controller
        {
            get
            {
                if (_controller == null)
                {
                    _controller = new ShimController(_debugLogger);
                }

                return _controller;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_controller != null)
                {
                    _controller.Dispose();
                    _controller = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}