using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using GrayscaleEffect;

namespace PackageExplorer
{
    public class GrayscaleMenuItem : MenuItem
    {

        //static GrayscaleMenuItem()
        //{
        //    //IconProperty.OverrideMetadata(
        //    //    typeof(GrayscaleMenuItem), 
        //    //    new UIPropertyMetadata(new PropertyChangedCallback(OnIconPropertyChanged)));
        //}

        //private static void OnIconPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        //{
        //    var menuItem = (GrayscaleMenuItem)sender;

        //    var icon = args.NewValue as Image;
        //    if (icon != null && icon.Effect == null)
        //    {
        //        icon.Effect = new GrayscaleEffect.GrayscaleEffect() { DesaturationFactor = 1 };
        //    }
        //}

        public GrayscaleMenuItem()
        {
            IsEnabledChanged += new System.Windows.DependencyPropertyChangedEventHandler(OnIsEnabledChanged);
        }

        void OnIsEnabledChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            var icon = Icon as Image;
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
