using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace NuGet.PowerShell.Commands.Test
{
    public class MockCommandRuntime : ICommandRuntime
    {

        private readonly List<object> _output;
        private readonly List<ErrorRecord> _errors;
        private readonly List<string> _warnings;

        // Methods
        public MockCommandRuntime(List<object> output, List<ErrorRecord> errors, List<string> warnings)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            _output = output;
            _errors = errors;
            _warnings = warnings;
        }

        public bool ShouldContinue(string query, string caption)
        {
            return true;
        }

        public bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll)
        {
            return true;
        }

        public bool ShouldProcess(string target)
        {
            return true;
        }

        public bool ShouldProcess(string target, string action)
        {
            return true;
        }

        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption)
        {
            return true;
        }

        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption, out ShouldProcessReason shouldProcessReason)
        {
            shouldProcessReason = ShouldProcessReason.None;
            return true;
        }

        public void ThrowTerminatingError(ErrorRecord errorRecord)
        {
            if (errorRecord.Exception != null)
            {
                throw errorRecord.Exception;
            }
            throw new InvalidOperationException(errorRecord.ToString());
        }

        public bool TransactionAvailable()
        {
            return false;
        }

        public void WriteCommandDetail(string text)
        {
        }

        public void WriteDebug(string text)
        {
        }

        public void WriteError(ErrorRecord errorRecord)
        {
            if (_errors != null)
            {
                _errors.Add(errorRecord);
            }
        }

        public void WriteObject(object sendToPipeline)
        {
            _output.Add(sendToPipeline);
        }

        public void WriteObject(object sendToPipeline, bool enumerateCollection)
        {
            if (!enumerateCollection)
            {
                _output.Add(sendToPipeline);
            }
            else
            {
                IEnumerator enumerator = LanguagePrimitives.GetEnumerator(sendToPipeline);
                if (enumerator != null)
                {
                    while (enumerator.MoveNext())
                    {
                        _output.Add(enumerator.Current);
                    }
                }
                else
                {
                    _output.Add(sendToPipeline);
                }
            }
        }

        public void WriteProgress(ProgressRecord progressRecord)
        {
        }

        public void WriteProgress(long sourceId, ProgressRecord progressRecord)
        {
        }

        public void WriteVerbose(string text)
        {
        }

        public void WriteWarning(string text)
        {
            if (_warnings != null)
            {
                _warnings.Add(text);
            }
        }

        public PSTransactionContext CurrentPSTransaction
        {
            get { throw new NotImplementedException(); }
        }

        public System.Management.Automation.Host.PSHost Host
        {
            get { throw new NotImplementedException(); }
        }
    }
}