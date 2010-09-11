using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace NuPack.Dialog.PackageManagerUI {
    public class StarControl : Control {
        private FrameworkElement _starPath;
        private double _value;

        static StarControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StarControl), new FrameworkPropertyMetadata(typeof(StarControl)));
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            _starPath = GetTemplateChild("StarPath") as FrameworkElement;
            _starPath.Loaded += new RoutedEventHandler(_starPath_Loaded);
        }

        void _starPath_Loaded(object sender, RoutedEventArgs e) {
            UpdateVisuals();
        }

        public void UpdateVisuals() {
            if (_starPath == null)
                return;

            if (_value < 0 || _value > 1)
                return;

            double width = _value * _starPath.ActualWidth;
            Rect rect = new Rect(0, 0, width, _starPath.ActualHeight);
            _starPath.Clip = new RectangleGeometry() { Rect = rect };
        }

        private double RoundValue(double value) {
            if (value < 0)
                value = 0;
            else if (value > 1)
                value = 1;
            return value;
        }

        public double Value {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// Value Dependency Property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
            "Value",
            typeof(double),
            typeof(StarControl),
            new PropertyMetadata(new PropertyChangedCallback(ValueChanged)));

        private static void ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            StarControl starControl = d as StarControl;
            if (starControl != null) {
                starControl._value = starControl.RoundValue((double)e.NewValue);
                starControl.UpdateVisuals();
            }
        }
    }
}
