using System.Collections.Generic;

namespace NuGetConsole {
    /// <summary>
    /// MEF interface to interact with the PowerConsole tool window.
    /// </summary>
    public interface IPowerConsoleWindow {
        /// <summary>
        /// Get all the host names available.
        /// </summary>
        IEnumerable<string> Hosts { get; }

        /// <summary>
        /// Get or set the active host in the tool window.
        /// </summary>
        string ActiveHost { get; set; }

        /// <summary>
        /// Gets or sets the active host setting.
        /// </summary>
        /// <value>The active host setting.</value>
        string ActiveHostSetting { get; set; }

        /// <summary>
        /// Show the tool window.
        /// </summary>
        void Show();
    }
}
