using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace NuGet.Dialog.PackageManagerUI
{
    public class TextBlockControl : TextBlock
    {
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TextBlockAutomationPeer(this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification="This class is instantiated right above. Don't you see it?")]
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
