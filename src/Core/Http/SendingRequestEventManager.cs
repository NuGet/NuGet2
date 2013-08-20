using System;
using System.Windows;

namespace NuGet
{
    // Implement weak event pattern. Read more here:
    // http://msdn.microsoft.com/en-us/library/aa970850(v=vs.100).aspx
    public class SendingRequestEventManager : WeakEventManager
    {
        public static void AddListener(IHttpClientEvents source, IWeakEventListener listener)
        {
            SendingRequestEventManager.CurrentManager.ProtectedAddListener(source, listener);
        }

        public static void RemoveListener(IHttpClientEvents source, IWeakEventListener listener)
        {
            SendingRequestEventManager.CurrentManager.ProtectedRemoveListener(source, listener);
        }

        private static SendingRequestEventManager CurrentManager
        {
            get
            {
                Type managerType = typeof(SendingRequestEventManager);
                SendingRequestEventManager manager = (SendingRequestEventManager)WeakEventManager.GetCurrentManager(managerType);
                if (manager == null)
                {
                    manager = new SendingRequestEventManager();
                    WeakEventManager.SetCurrentManager(managerType, manager);
                }
                return manager;
            }
        } 

        protected override void StartListening(object source)
        {
            var clientEvents = (IHttpClientEvents)source;
            clientEvents.SendingRequest += OnSendingRequest;
        }

        protected override void StopListening(object source)
        {
            var clientEvents = (IHttpClientEvents)source;
            clientEvents.SendingRequest -= OnSendingRequest;
        }

        private void OnSendingRequest(object sender, WebRequestEventArgs e)
        {
            base.DeliverEvent(sender, e);
        }
    }
}
