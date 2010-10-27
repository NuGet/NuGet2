namespace NuGetConsole {
    /// <summary>
    /// MEF interface for host provider. PowerConsole host providers must export this
    /// interface implementation and decorate it with a HostName attribute.
    /// </summary>
    public interface IHostProvider {
        /// <summary>
        /// Create a new host instance.
        /// </summary>
        /// <param name="console">The console for the host to use. The host may output text
        /// to the console in command execution.</param>
        /// <returns>A new host instance.</returns>
        IHost CreateHost(IConsole console);
    }
}
