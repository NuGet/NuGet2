using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace NuGet.VisualStudio.Test {
    public class MockCommandRuntime : ICommandRuntime {
        private readonly List<object> _output;

        // Methods
        public MockCommandRuntime(List<object> outputList) {
            if (outputList == null) {
                throw new ArgumentNullException("outputList");
            }
            _output = outputList;
        }

        public bool ShouldContinue(string query, string caption) {
            return true;
        }

        public bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll) {
            return true;
        }

        public bool ShouldProcess(string target) {
            return true;
        }

        public bool ShouldProcess(string target, string action) {
            return true;
        }

        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption) {
            return true;
        }

        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption, out ShouldProcessReason shouldProcessReason) {
            shouldProcessReason = ShouldProcessReason.None;
            return true;
        }

        public void ThrowTerminatingError(ErrorRecord errorRecord) {
            if (errorRecord.Exception != null) {
                throw errorRecord.Exception;
            }
            throw new InvalidOperationException(errorRecord.ToString());
        }

        public bool TransactionAvailable() {
            return false;
        }

        public void WriteCommandDetail(string text) {
        }

        public void WriteDebug(string text) {
        }

        public void WriteError(ErrorRecord errorRecord) {
            if (errorRecord.Exception != null) {
                throw new InvalidOperationException(errorRecord.Exception.Message, errorRecord.Exception);
            }
            throw new InvalidOperationException(errorRecord.ToString());
        }

        public void WriteObject(object sendToPipeline) {
            this._output.Add(sendToPipeline);
        }

        public void WriteObject(object sendToPipeline, bool enumerateCollection) {
            if (!enumerateCollection) {
                this._output.Add(sendToPipeline);
            }
            else {
                IEnumerator enumerator = LanguagePrimitives.GetEnumerator(sendToPipeline);
                if (enumerator != null) {
                    while (enumerator.MoveNext()) {
                        this._output.Add(enumerator.Current);
                    }
                }
                else {
                    this._output.Add(sendToPipeline);
                }
            }
        }

        public void WriteProgress(ProgressRecord progressRecord) {
        }

        public void WriteProgress(long sourceId, ProgressRecord progressRecord) {
        }

        public void WriteVerbose(string text) {
        }

        public void WriteWarning(string text) {
        }

        public PSTransactionContext CurrentPSTransaction {
            get { throw new NotImplementedException(); }
        }

        public System.Management.Automation.Host.PSHost Host {
            get { throw new NotImplementedException(); }
        }
    }


}
