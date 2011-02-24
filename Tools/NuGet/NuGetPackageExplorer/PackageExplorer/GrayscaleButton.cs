using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace PackageExplorer
{
    class GrayscaleButton : Button
    {
        public GrayscaleButton()
        {
            IsEnabledChanged += new System.Windows.DependencyPropertyChangedEventHandler(OnIsEnabledChanged);
        }

        void OnIsEnabledChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            var icon = Content as Image;
            if (icon != null)
            {
                var effect = icon.Effect as GrayscaleEffect.GrayscaleEffect;
                if (effect != null)
                {
                    bool isEnabled = (bool)e.NewValue;
                    effect.DesaturationFactor = isEnabled ? 1 : 0;
                }
            }
        }
    }
}
