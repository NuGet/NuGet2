using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.VisualStudio
{
    public class VsDialogWindow : Microsoft.VisualStudio.PlatformUI.DialogWindow
    {
        public VsDialogWindow()
        {
        }

        public VsDialogWindow(string helpTopic)
            : base(helpTopic)
        {
        }
    }

    public static class VsBrushes
    {
        public static object InfoBackgroundKey
        {
            get
            {
                return Microsoft.VisualStudio.Shell.VsBrushes.InfoBackgroundKey;
            }
        }

        public static object InfoTextKey
        {
            get
            {
                return Microsoft.VisualStudio.Shell.VsBrushes.InfoTextKey;
            }
        }

        public static object EnvironmentBackgroundGradientKey
        {
            get
            {
                return Microsoft.VisualStudio.Shell.VsBrushes.EnvironmentBackgroundGradientKey;
            }
        }

        public static object NewProjectBackgroundKey
        {
            get
            {
                return Microsoft.VisualStudio.Shell.VsBrushes.NewProjectBackgroundKey;
            }
        }

        public static object WindowTextKey
        {
            get
            {
                return Microsoft.VisualStudio.Shell.VsBrushes.WindowTextKey;
            }
        }

        public static object WindowKey
        {
            get
            {
                return Microsoft.VisualStudio.Shell.VsBrushes.WindowKey;
            }
        }

        public static object ActiveBorderKey
        {
            get
            {
                return Microsoft.VisualStudio.Shell.VsBrushes.ActiveBorderKey;
            }
        }

        public static object ControlLinkTextKey
        {
            get
            {
                return Microsoft.VisualStudio.Shell.VsBrushes.ControlLinkTextKey;
            }
        }

        public static object ControlLinkTextHoverKey
        {
            get
            {
                return Microsoft.VisualStudio.Shell.VsBrushes.ControlLinkTextHoverKey;
            }
        }
    }
}
