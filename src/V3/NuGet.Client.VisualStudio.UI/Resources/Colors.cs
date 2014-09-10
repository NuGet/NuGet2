using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace NuGet.Client.VisualStudio.UI.Resources
{
    public static class Brushes
    {
        public static object HeaderBackground
        {
            get
            {
                return VsBrushes.BrandedUIBackgroundKey;
            }
        }

        public static object ListPaneBackground
        {
            get
            {
                return VsBrushes.BrandedUIFillKey;
            }
        }

        public static object DetailPaneBackground
        {
            get
            {
                return VsBrushes.BrandedUIFillKey;
            }
        }

        public static object LegalMessageBackground
        {
            get
            {
                return VsBrushes.BrandedUIBackgroundKey;
            }
        }

        public static object UIText
        {
            get
            {
                return VsBrushes.BrandedUITextKey;
            }
        }

        public static object ComboBoxBackground
        {
            get
            {
                return VsBrushes.ComboBoxBackgroundKey;
            }
        }
    }
}
