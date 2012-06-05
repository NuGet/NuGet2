using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace NuGet.Dialog.PackageManagerUI
{
    public class TextBlockControl : TextBlock
    {
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TextBlockControlAutomationPeer(this);
        }

        private class TextBlockControlAutomationPeer : TextBlockAutomationPeer
        {
            public TextBlockControlAutomationPeer(TextBlock owner) 
                : base(owner)
            {
            }

            protected override bool IsContentElementCore()
            {
                return true;
            }
        }
    }
}
