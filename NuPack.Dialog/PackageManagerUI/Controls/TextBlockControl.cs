using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace NuGet.Dialog.PackageManagerUI {
    /// <summary>
    /// This control exists only for providing automation/accessibility support for the TextBlock
    /// class when inside a DataTemplate.
    /// </summary
    public class TextBlockControl : TextBlock {
        protected override AutomationPeer OnCreateAutomationPeer() {
            return new TextBlockControlAutomationPeer(this);
        }
    }

    class TextBlockControlAutomationPeer : TextBlockAutomationPeer {
        public TextBlockControlAutomationPeer(TextBlock owner) : base(owner) { }

        protected override bool IsControlElementCore() {
            return true;
        }
    }
}
