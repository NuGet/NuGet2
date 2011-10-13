using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TemplateWizard;

namespace NuGet.VisualStudio
{
    [ComImport]
    [Guid("D6DEA71B-4A42-4B55-8A59-3191B91EF36E")]
    public interface IVsTemplateWizard : IWizard
    {
    }
}
