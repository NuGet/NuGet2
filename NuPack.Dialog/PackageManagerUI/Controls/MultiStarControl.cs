using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

[assembly: ThemeInfo(ResourceDictionaryLocation.SourceAssembly, ResourceDictionaryLocation.SourceAssembly)]

namespace NuPack.Dialog.PackageManagerUI {
    public partial class MultiStarControl : RangeBase {
        /// <summary>
        /// Container which holds the stars
        /// </summary>
        private StackPanel RootElement;
        private ControlTemplate StarTemplate;

        static MultiStarControl() {
            //Set so that individual StarControls don't get focus.
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(MultiStarControl), new FrameworkPropertyMetadata(KeyboardNavigationMode.None));

            //Not Focusable by default
            FocusableProperty.OverrideMetadata(typeof(MultiStarControl), new FrameworkPropertyMetadata(false));

            RangeBase.MaximumProperty.OverrideMetadata(typeof(MultiStarControl), new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
            RangeBase.ValueProperty.OverrideMetadata(typeof(MultiStarControl), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiStarControl), new FrameworkPropertyMetadata(typeof(MultiStarControl)));
        }

        public MultiStarControl() {
        }

        protected override void OnValueChanged(double oldValue, double newValue) {
            base.OnValueChanged(oldValue, newValue);
            UpdateStarList();
        }

        protected override void OnMaximumChanged(double oldMaximum, double newMaximum) {
            base.OnMaximumChanged(oldMaximum, newMaximum);
            UpdateStarList();
        }

        private void UpdateStarList() {
            if (RootElement != null) {
                RootElement.Children.Clear();
                for (int i = 0; i < Maximum; ++i) {
                    StarControl star = new StarControl();
                    star.Value = Value - i;
                    star.Template = StarTemplate;
                    RootElement.Children.Add(star);
                }
            }
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            if (this.RootElement != null) {
                this.RootElement.SizeChanged -= new SizeChangedEventHandler(RootElement_SizeChanged);
            }

            RootElement = (StackPanel)Template.FindName("RootElement", this);
            StarTemplate = RootElement.TryFindResource(new ComponentResourceKey(typeof(MultiStarControl), "StarTemplate")) as ControlTemplate;
            UpdateStarList();

            if (this.RootElement != null) {
                this.RootElement.SizeChanged += new SizeChangedEventHandler(RootElement_SizeChanged);
            }

        }

        void RootElement_SizeChanged(object sender, SizeChangedEventArgs e) {
            foreach (StarControl star in RootElement.Children) {
                star.UpdateVisuals();
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer() {
            return new RangeBaseAutomationPeer(this);
        }
    }
}
