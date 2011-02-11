using System;

namespace NuGetConsole {
    /// <summary>
    /// Interface to access more properties of wpf console.
    /// </summary>
    public interface IWpfConsole : IConsole {
        /// <summary>
        /// Get the console UIElement to be used as Content for a VS tool window.
        /// </summary>
        object Content { get; }

        /// <summary>
        /// Tells the Wpf console to update its state when command is executing.
        /// </summary>
        void SetExecutionMode(bool isExecuting);

        /// <summary>
        /// Get the editor's IVsTextView for further direct interaction.
        /// </summary>
        object VsTextView { get; }
    }
}
