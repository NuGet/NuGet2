using System;
using System.Diagnostics;

namespace NuPackConsole.Implementation.PowerConsole
{
    /// <summary>
    /// Represents a host with extra info.
    /// </summary>
    class HostInfo : ObjectWithFactory<PowerConsoleWindow>
    {
        Lazy<IHostProvider, IHostMetadata> HostProvider { get; set; }

        public HostInfo(PowerConsoleWindow factory, Lazy<IHostProvider, IHostMetadata> hostProvider)
            : base(factory)
        {
            UtilityMethods.ThrowIfArgumentNull(hostProvider);
            this.HostProvider = hostProvider;
        }

        /// <summary>
        /// Get the HostName attribute value of this host.
        /// </summary>
        public string HostName
        {
            get { return HostProvider.Metadata.HostName; }
        }

        string _displayName;

        /// <summary>
        /// Get the DisplayName value of this host.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (_displayName == null)
                {
                    try
                    {
                        //TODO: parse multi-culture DisplayName
                        _displayName = HostProvider.Metadata.DisplayName;
                    }
                    catch (Exception x)
                    {
                        Trace.TraceError(x.ToString());
                        _displayName = HostName;
                    }
                }
                return _displayName;
            }
        }

        IWpfConsole _wpfConsole;

        /// <summary>
        /// Get/create the console for this host. If not already created, this
        /// actually creates the (console, host) pair.
        /// 
        /// Note: Creating the console is handled by this package and mostly will
        /// succeed. However, creating the host could be from other packages and
        /// fail. In that case, this console is already created and can be used
        /// subsequently in limited ways, such as displaying an error message.
        /// </summary>
        public IWpfConsole WpfConsole
        {
            get
            {
                if (_wpfConsole == null)
                {
                    _wpfConsole = Factory.WpfConsoleService.CreateConsole(
                        Factory.ServiceProvider, PowerConsoleWindow.ContentType, HostName);
                    _wpfConsole.Host = HostProvider.Value.CreateHost(_wpfConsole);
                }
                return _wpfConsole;
            }
        }
    }
}
