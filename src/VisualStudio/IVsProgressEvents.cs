﻿using System;

namespace NuGet.VisualStudio {
    public interface IVsProgressEvents {
        event EventHandler<ReportProgressEventArgs> ProgressAvailable;
    }
}
