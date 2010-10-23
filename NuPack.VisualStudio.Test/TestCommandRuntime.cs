using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Collections;
using System.Reflection;

namespace NuPack.VisualStudio.Test {
    public class TestCommandRuntime : ICommandRuntime {
        private List<object> output;

        // Methods
        public TestCommandRuntime(List<object> outputArrayList) {
            if (outputArrayList == null) {
                throw new ArgumentNullException("outputArrayList");
            }
            this.output = outputArrayList;
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
            this.output.Add(sendToPipeline);
        }

        public void WriteObject(object sendToPipeline, bool enumerateCollection) {
            if (!enumerateCollection) {
                this.output.Add(sendToPipeline);
            }
            else {
                IEnumerator enumerator = LanguagePrimitives.GetEnumerator(sendToPipeline);
                if (enumerator != null) {
                    while (enumerator.MoveNext()) {
                        this.output.Add(enumerator.Current);
                    }
                }
                else {
                    this.output.Add(sendToPipeline);
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

        public static IList<object> ExecutePSCmdlet(PSCmdlet cmdlet) {
            var output = new List<object>();
            var runtime = new TestCommandRuntime(output);

            var type = typeof(PSCmdlet);
            
            type.GetMethod("BeginProcessing", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(cmdlet, null);
            type.GetMethod("ProcessRecord", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(cmdlet, null);
            type.GetMethod("EndProcessing", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(cmdlet, null);

            return output;
        }

        public PSTransactionContext CurrentPSTransaction {
            get { throw new NotImplementedException(); }
        }

        public System.Management.Automation.Host.PSHost Host {
            get { throw new NotImplementedException(); }
        }
    }


}
