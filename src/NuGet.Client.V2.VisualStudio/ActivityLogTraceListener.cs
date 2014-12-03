using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.Client.VisualStudio
{
    public class ActivityLogTraceListener : TraceListener
    {
        private Dictionary<TraceEventType, __ACTIVITYLOG_ENTRYTYPE> _eventTypeMappings = new Dictionary<TraceEventType, __ACTIVITYLOG_ENTRYTYPE>()
        {
            { TraceEventType.Information, __ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION },
            { TraceEventType.Error, __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR },
            { TraceEventType.Warning, __ACTIVITYLOG_ENTRYTYPE.ALE_WARNING }
        };

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            __ACTIVITYLOG_ENTRYTYPE type;
            if (_eventTypeMappings.TryGetValue(eventType, out type))
            {
                GetActivityLog().LogEntry((uint)type, source, message);
            }
        }

        public override void Write(string message)
        {
        }

        public override void WriteLine(string message)
        {
        }

        private static IVsActivityLog GetActivityLog()
        {
            return ServiceLocator.GetGlobalService<SVsActivityLog, IVsActivityLog>();
        }
    }
}
