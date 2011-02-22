using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;

namespace NuGet {
    public class HttpClient : IHttpClient, IObservable<int> {
        private const int RequestTimeOut = 5000;
        private readonly HashSet<IObserver<int>> _observers = new HashSet<IObserver<int>>();

        public string UserAgent {
            get;
            set;
        }

        public WebRequest CreateRequest(Uri uri) {
            WebRequest request = WebRequest.Create(uri);
            InitializeRequest(request);
            return request;
        }

        public void InitializeRequest(WebRequest request) {
            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null) {
                httpRequest.UserAgent = UserAgent;
            }

            request.UseDefaultCredentials = true;
            if (request.Proxy != null) {
                // If we are going through a proxy then just set the default credentials
                request.Proxy.Credentials = CredentialCache.DefaultCredentials;
            }

            // Don't do this in debug mode so we can debug requests without worrying about timeouts
#if !DEBUG
            request.Timeout = RequestTimeOut;
#endif
        }

        public Uri GetRedirectedUri(Uri uri) {
            WebRequest request = CreateRequest(uri);
            using (WebResponse response = request.GetResponse()) {
                if (response == null) {
                    return null;
                }
                return response.ResponseUri;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public byte[] DownloadData(Uri uri) {

            const int ChunkSize = 1024 * 4; // 4KB

            WebRequest request = HttpWebRequest.Create(uri);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            int length = (int)response.ContentLength;

            byte[] buffer = new byte[(int)length];
            int totalReadSoFar = 0;

            using (Stream stream = response.GetResponseStream()) {
                try {
                    while (totalReadSoFar < length) {
                        int bytesRead = stream.Read(buffer, totalReadSoFar, Math.Min(length - totalReadSoFar, ChunkSize));
                        if (bytesRead == 0) {
                            break;
                        }
                        else {
                            totalReadSoFar += bytesRead;
                            IterateObservers(o => o.OnNext((totalReadSoFar * 100) / length));
                        }
                    }

                }
                catch (Exception ex) {
                    IterateObservers(o => o.OnError(ex));
                }
            }

            response.Close();

            IterateObservers(o => o.OnCompleted());

            return buffer;
        }

        public IDisposable Subscribe(IObserver<int> observer) {
            if (observer == null) {
                throw new ArgumentNullException("observer");
            }

            _observers.Add(observer);
            
            Action dispose = () => _observers.Remove(observer);
            return new SubscriberDisposable(dispose);
        }

        private void IterateObservers(Action<IObserver<int>> action) {
            // We can't enumerate directly on _observers because the invoked action 
            // can potentially remove the subscribe, which modifies _observers.
            var temp = _observers.ToList();
            foreach (var observer in temp) {
                action(observer);
            }
        }

        private class SubscriberDisposable : IDisposable {
            private Action _disposeAction;

            public SubscriberDisposable(Action disposeAction) {
                _disposeAction = disposeAction;
            }

            public void Dispose() {
                _disposeAction.Invoke();
            }
        }
    }
}