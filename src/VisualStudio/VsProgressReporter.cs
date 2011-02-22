﻿using System;
using System.ComponentModel.Composition;

namespace NuGet.VisualStudio {

    [Export(typeof(IProgressReporter))]
    [Export(typeof(IVsProgressEvents))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class VsProgressReporter : IProgressReporter, IVsProgressEvents {

        public event EventHandler<ReportProgressEventArgs> ProgressAvailable;
        private readonly IServiceProvider _serviceProvider;

        public VsProgressReporter()
            : this(ServiceLocator.GetInstance<IServiceProvider>()) {
        }

        public VsProgressReporter(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
        }

        public void ReportProgress(string operation, int percentComplete) {
            if (operation == null) {
                throw new ArgumentNullException("operation");
            }

            percentComplete = Math.Max(0, percentComplete);
            percentComplete = Math.Min(100, percentComplete);

            if (ProgressAvailable != null) {
                ProgressAvailable(this, new ReportProgressEventArgs(operation, percentComplete));
            }
        }
    }
}