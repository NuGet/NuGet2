﻿using System;
using System.ComponentModel.Composition;

namespace NuGet.VisualStudio {
    [Export(typeof(IProgressReporter))]
    [Export(typeof(IProgressProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class VsProgressReporter : IProgressReporter, IProgressProvider {
        private readonly IServiceProvider _serviceProvider;

        public event EventHandler<ProgressEventArgs> ProgressAvailable;

        public VsProgressReporter()
            : this(ServiceLocator.GetInstance<IServiceProvider>()) {
        }

        public VsProgressReporter(IServiceProvider serviceProvider) {
            if (serviceProvider == null) {
                throw new ArgumentNullException("serviceProvider");
            }
            _serviceProvider = serviceProvider;
        }

        public void ReportProgress(string operation, int percentComplete) {
            if (operation == null) {
                throw new ArgumentNullException("operation");
            }

            percentComplete = Math.Max(0, percentComplete);
            percentComplete = Math.Min(100, percentComplete);

            if (ProgressAvailable != null) {
                ProgressAvailable(this, new ProgressEventArgs(operation, percentComplete));
            }
        }
    }
}