using System;

namespace Microsoft.VisualStudio.Project
{
    [Flags]
    public enum ProjectAccess
    {
        None = 0,
        Read = 1,
        Write = 3,
        UpgradeableRead = 5,
        LockMask = 7,
        SkipInitialEvaluation = 8,
        SuppressReevaluation = 16,
        StickyWrite = 37,
        OptionMask = 2147483640,
    }
}
