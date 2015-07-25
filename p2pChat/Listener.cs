using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using TakeAshUtility;

namespace p2pChat {

    public class Listener :
        IDisposable {

        private const int BufferSize = 1024;
        private static readonly char[] WhiteSpaces = new[] { ' ', '\n', '\r', '\t', '\0', };

        private static Properties.Settings _settings = Properties.Settings.Default;

        private bool disposed = false;
        private TextBox _log;
        private TcpListenerEx _listener;
        private BackgroundWorker _worker;

        public Listener(TextBox log) {
            _log = log;
            _listener = new TcpListenerEx(IPAddress.IPv6Any, _settings.Port);
            _listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0);
            _worker = CreateWorker();
        }

        public bool IsBusy {
            get { return _listener.Active; }
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
                while (true) {
                    if (e.Cancel || !_listener.Active) {
                        break;
                    }
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
                _log.Text += message + "\n";
            };
            worker.RunWorkerCompleted += (sender, e) => {
            };
            return worker;
        }

        private void ShowMessage(string message) {
            if (!_worker.IsBusy) {
                return;
            }
            _worker.ReportProgress(0, message);
        }

        private void HandleClient(Object state) {
            using (var client = _listener.AcceptTcpClient())
            using (var ns = client.GetStream()) {
                if (ns.CanTimeout) {
                    ns.ReadTimeout = _settings.NetworkTimeout * 1000;
                    ns.WriteTimeout = _settings.NetworkTimeout * 1000;
                }
                var endPoint = client.Client.RemoteEndPoint as IPEndPoint;
                var clientAddress = endPoint != null ?
                    endPoint.Address.ToString() + ":" + endPoint.Port.ToString() :
                    null;
                ShowMessage("Connected: " + clientAddress);
                try {
                    var isDisconnected = false;
                    while (!isDisconnected && _listener.Active) {
                        if (!ns.DataAvailable) {
                            Thread.Sleep(100);
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
                        ShowMessage(message.Trim(WhiteSpaces));
                        if (!isDisconnected) {
                            var response = "Received: " + message.Length.ToString() + "\r\n\0";
                            var sendBuffer = Encoding.UTF8.GetBytes(response);
                            ns.Write(sendBuffer, 0, sendBuffer.Length);
                        }
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex) {
                    var socketException = ex.InnerException as SocketException;
                    var message = socketException == null ?
                        ex.GetAllMessages() :
                        ((SocketError)socketException.ErrorCode).ToString() + ": " + clientAddress;
                    ShowMessage(message);
                }
                ShowMessage("Disconnected: " + clientAddress);
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
        }

        #endregion
    }
}
