using System;
using System.Threading;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace PingPong {

    public delegate void SocketUpdate(string Message, string status);

    class Application {

        static void OnMessage(string message, string status) {
            Console.WriteLine(message + status);
        }

        static void Main(string[] args) {
            SocketUpdate d = new SocketUpdate(OnMessage);
            Socket socket = new Socket(new SimpleSyncObject(), d);
            socket.StartProcess();
            Thread.Sleep(10000);
        }

    }

    class Socket {
        private static SocketUpdate _status;
        private static Thread _thread;
        private static ISynchronizeInvoke _synch;
        private static Collection<string> _listItems = new Collection<string>();

        public Socket(ISynchronizeInvoke syn, SocketUpdate notify) {
            _synch = syn;
            _status = notify;
        }

        public void StartProcess() {
            _thread = new Thread(AddItemsToList);
            _thread.IsBackground = true;
            _thread.Name = "Add List Items Thread";
            _thread.Start();
        }

        private static void AddItemsToList() {
            for (int i = 0; i <= 100; i++) {
                lock (_listItems)
                    _listItems.Add("List Item #: " + _listItems.Count);
                UpdateStatus("Received...", "" + i);
                Thread.Sleep(500);
            }
        }

        private static void UpdateStatus(string msg, string status) {
            object[] items = new object[2];
            items[0] = msg;
            items[1] = status;
            _synch.Invoke(_status, items);
        }

    }

    class SimpleSyncObject : ISynchronizeInvoke {
        private readonly object _sync;
        public SimpleSyncObject() {
            _sync = new object();
        }
        public IAsyncResult BeginInvoke(Delegate method, object[] args) {
            var result = new SimpleAsyncResult();
            ThreadPool.QueueUserWorkItem(delegate {
                result.AsyncWaitHandle = new ManualResetEvent(false);
                try {
                    result.AsyncState = Invoke(method, args);
                } catch (Exception exception) {
                    result.Exception = exception;
                }
                result.IsCompleted = true;
            });
            return result;
        }

        public object EndInvoke(IAsyncResult result) {
            if (!result.IsCompleted) {
                result.AsyncWaitHandle.WaitOne();
            }
            return result.AsyncState;
        }

        public object Invoke(Delegate method, object[] args) {
            lock (_sync) {
                return method.DynamicInvoke(args);
            }
        }

        public bool InvokeRequired {
            get { return true; }
        }
    }

    class SimpleAsyncResult : IAsyncResult {
        object _state;
        public bool IsCompleted { get; set; }
        public WaitHandle AsyncWaitHandle { get; internal set; }
        public object AsyncState {
            get {
                if (Exception != null) {
                    throw Exception;
                }
                return _state;
            }
            internal set {
                _state = value;
            }
        }
        public bool CompletedSynchronously { get { return IsCompleted; } }
        internal Exception Exception { get; set; }
    }

}
