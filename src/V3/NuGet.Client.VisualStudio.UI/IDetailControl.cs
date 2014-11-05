using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Resolution;

namespace NuGet.Client.VisualStudio.UI
{
    // The control in the right pane implements this interface.
    interface IDetailControl
    {
        /// <summary>
        /// Resolves the package actions from the current user action.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<PackageAction>> ResolveActionsAsync();

        /// <summary>
        /// Refreshes the control after package install/uninstall.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Returns the user selected file conflict action.
        /// </summary>
        FileConflictAction FileConflictAction { get; }
    }
}
