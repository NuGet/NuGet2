using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace NuGet.WebMatrix.Tests.Utilities
{
    public class TemporaryDispatcherThread : IDisposable
    {
        public TemporaryDispatcherThread()
        {
            this.SyncEvent = new ManualResetEvent(initialState: false);

            this.Thread = new Thread(this.Run);
            this.Thread.SetApartmentState(ApartmentState.STA);
            this.Thread.IsBackground = false;

            this.Thread.Start();

            this.SyncEvent.WaitOne(TimeSpan.FromSeconds(5));
        }

        public Dispatcher Dispatcher
        {
            get;
            private set;
        }

        private DispatcherFrame Frame
        {
            get;
            set;
        }

        public TaskScheduler Scheduler
        {
            get;
            private set;
        }

        private ManualResetEvent SyncEvent
        {
            get;
            set;
        }

        private Thread Thread
        {
            get;
            set;
        }

        public void Dispose()
        {
            this.Dispatcher.Invoke((Action)(() => { this.Frame.Continue = false; }));
            this.Thread.Join();
            this.SyncEvent.Dispose();
        }

        public void Invoke(Action action)
        {
            Exception thrown = null;
            this.Dispatcher.Invoke((Action)(() => 
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    thrown = ex;
                }
            }));

            if (thrown != null)
            {
                throw thrown;
            }
        }

        private void Run()
        {
            this.Dispatcher = Dispatcher.CurrentDispatcher;
            this.Frame = new DispatcherFrame();

            this.Dispatcher.BeginInvoke((Action)this.Initialize);

            Dispatcher.PushFrame(this.Frame);
        }

        private void Initialize()
        {
            this.Scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            this.SyncEvent.Set();
        }
    }
}
