using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using TakeAshUtility;

namespace p2pChat {

    public class Listener :
        IDisposable,
        INotifyPropertyChangedWithValue {

        private const int BufferSize = 1024;
        private static readonly char[] WhiteSpaces = new[] { ' ', '\n', '\r', '\t', '\0', };

        private bool disposed = false;
        private PropertyChangedWithValueEventHandler _propertyChangedHandler;
        private TcpListenerEx _listener;
        private BackgroundWorker _worker;
        private string _message = null;

        public Listener(
            int port,
            int timeout,
            PropertyChangedWithValueEventHandler propertyChangedHandler = null
        ) {
            Port = port;
            Timeout = timeout;
            if (propertyChangedHandler != null) {
                _propertyChangedHandler = propertyChangedHandler;
                this.PropertyChangedWithValue += propertyChangedHandler;
            }
            _listener = new TcpListenerEx(IPAddress.IPv6Any, Port);
            _listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0);
            _worker = CreateWorker();
        }

        public int Port { get; private set; }
        public int Timeout { get; private set; }

        public bool IsBusy {
            get { return _listener.Active; }
        }

        public string Message {
            get { return _message; }
            private set {
                _message = value;
                this.NotifyPropertyChanged("Message", _message);
            }
        }

        public void Start() {
            if (_listener.Active) {
                return;
            }
            _listener.Start();
            _worker.RunWorkerAsync();
        }

        public void Stop() {
            if (!_listener.Active) {
                return;
            }
            _worker.CancelAsync();
            _listener.Stop();
        }

        private BackgroundWorker CreateWorker() {
            var worker = new BackgroundWorker() {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true,
            };
            worker.DoWork += (sender, e) => {
                while (_listener.Active && !e.Cancel) {
                    if (_listener.Pending()) {
                        ThreadPool.QueueUserWorkItem(HandleClient);
                    }
                    Thread.Sleep(100);
                }
            };
            worker.ProgressChanged += (sender, e) => {
                var message = e.UserState as string;
                if (String.IsNullOrEmpty(message)) {
                    return;
                }
                Message = message;
            };
            worker.RunWorkerCompleted += (sender, e) => {
            };
            return worker;
        }

        private void HandleClient(Object state) {
            using (var client = _listener.AcceptTcpClient())
            using (var ns = client.GetStream()) {
                if (ns.CanTimeout) {
                    ns.ReadTimeout = Timeout * 1000;
                    ns.WriteTimeout = Timeout * 1000;
                }
                var endPoint = client.Client.RemoteEndPoint as IPEndPoint;
                var clientAddress = endPoint != null ?
                    endPoint.Address.ToString() + ":" + endPoint.Port.ToString() :
                    null;
                Message = "Connected: " + clientAddress;
                try {
                    var isDisconnected = false;
                    while (!isDisconnected &&
                        client.GetState() == TcpState.Established &&
                        _listener.Active) {
                        Thread.Sleep(100);
                        if (!ns.DataAvailable) {
                            continue;
                        }
                        var message = "";
                        using (var ms = new MemoryStream()) {
                            var receiveBuffer = new byte[BufferSize];
                            do {
                                var receiveSize = ns.Read(receiveBuffer, 0, receiveBuffer.Length);
                                if (receiveSize == 0) {
                                    isDisconnected = true;
                                    break;
                                }
                                ms.Write(receiveBuffer, 0, receiveSize);
                            } while (ns.DataAvailable);
                            if (!isDisconnected) {
                                message = Encoding.UTF8
                                    .GetString(ms.GetBuffer(), 0, (int)ms.Length);
                            }
                        }
                        Message = message.Trim(WhiteSpaces);
                        if (!isDisconnected) {
                            var response = "Received: " + message.Length.ToString() + "\r\n\0";
                            var sendBuffer = Encoding.UTF8.GetBytes(response);
                            ns.Write(sendBuffer, 0, sendBuffer.Length);
                        }
                    }
                }
                catch (Exception ex) {
                    var socketException = ex.InnerException as SocketException;
                    var message = socketException == null ?
                        ex.GetAllMessages() :
                        ((SocketError)socketException.ErrorCode).ToString() + ": " + clientAddress;
                    Message = message;
                }
                Message = "Disconnected: " + clientAddress;
            }
        }

        #region IDisposable

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposed) {
                return;
            }
            if (disposing) {
                // Free any other managed objects here.
                _worker.CancelAsync();
                _listener.Stop();
            }
            // Free any unmanaged objects here.
            disposed = true;
        }

        ~Listener() {
            Dispose(false);
            if (_propertyChangedHandler != null) {
                this.PropertyChangedWithValue -= _propertyChangedHandler;
                _propertyChangedHandler = null;
            }
        }

        #endregion

        #region INotifyPropertyChangedWithValue members
        #pragma warning disable 0067

        public event PropertyChangedWithValueEventHandler PropertyChangedWithValue;

        #pragma warning restore 0067
        #endregion
    }
}
