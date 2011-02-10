using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGetConsole {

    public class ProgressData {

        public int PercentComplete { get; private set; }
        public string CurrentOperation { get; private set; }

        public ProgressData(string currentOperation, int percentComplete) {
            if (percentComplete < 0 || percentComplete > 100) {
                throw new ArgumentOutOfRangeException("percentComplete");
            }

            if (currentOperation == null) {
                throw new ArgumentNullException("currentOperation");
            }

            PercentComplete = percentComplete;
            CurrentOperation = currentOperation;
        }

    }
}