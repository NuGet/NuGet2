﻿using System;

namespace NuGet.VisualStudio {
    public class ReportProgressEventArgs : EventArgs {
        public string Operation { get; private set; }
        public int PercentComplete { get; private set; }

        public ReportProgressEventArgs(string operation, int percentComplete) {
            Operation = operation;
            PercentComplete = percentComplete;
        }
    }
}
