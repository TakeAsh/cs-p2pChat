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

    public class Talker :
        IDisposable,
        INotifyPropertyChanged {

        private const int BufferSize = 1024;
        private static readonly char[] WhiteSpaces = new[] { ' ', '\n', '\r', '\t', '\0', };

        private static Properties.Settings _settings = Properties.Settings.Default;

        private bool disposed = false;
        private TextBox _log;
        private TcpClient _client;
        private NetworkStream _ns;
        private BackgroundWorker _worker;

        public Talker(
            string host,
            int port,
            TextBox log,
            PropertyChangedEventHandler propertyChangedHandler = null
        ) {
            Host = host;
            Port = port;
            _log = log;
            if (propertyChangedHandler != null) {
                this.PropertyChanged += propertyChangedHandler;
            }
            _client = new TcpClient(Host, Port);
            _ns = _client.GetStream();
            if (_ns.CanTimeout) {
                _ns.ReadTimeout = _settings.NetworkTimeout * 1000;
                _ns.WriteTimeout = _settings.NetworkTimeout * 1000;
            }
            _worker = CreateWorker();
            NotifyPropertyChanged("Connected");
        }

        public string Host { get; private set; }
        public int Port { get; private set; }
        public bool Connected {
            get { return _client.GetState() == TcpState.Established; }
        }

        public void Talk(string message) {
            if (_ns == null || !_ns.CanWrite) {
                return;
            }
            var sendBytes = Encoding.UTF8.GetBytes(message + "\r\n\0");
            _ns.Write(sendBytes, 0, sendBytes.Length);
        }

        private BackgroundWorker CreateWorker() {
            var worker = new BackgroundWorker() {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true,
            };
            worker.DoWork += (sender, e) => {
                while (Connected && _ns.CanRead && !e.Cancel) {
                    if (_ns.DataAvailable) {
                        HandleClient();
                    }
                    Thread.Sleep(100);
                }
                NotifyPropertyChanged("Connected");
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
            worker.RunWorkerAsync();
            return worker;
        }

        private void ShowMessage(string message) {
            if (!_worker.IsBusy) {
                return;
            }
            _worker.ReportProgress(0, message);
        }

        private void HandleClient() {
            try {
                var isDisconnected = false;
                var message = "";
                using (var ms = new MemoryStream()) {
                    var receiveBuffer = new byte[BufferSize];
                    do {
                        var receiveSize = _ns.Read(receiveBuffer, 0, receiveBuffer.Length);
                        if (receiveSize == 0) {
                            isDisconnected = true;
                            break;
                        }
                        ms.Write(receiveBuffer, 0, receiveSize);
                    } while (_ns.DataAvailable);
                    message = isDisconnected ?
                        "Disconnected" :
                        Encoding.UTF8
                            .GetString(ms.GetBuffer(), 0, (int)ms.Length);
                }
                ShowMessage(message.Trim(WhiteSpaces));
            }
            catch (Exception ex) {
                var socketException = ex.InnerException as SocketException;
                var message = socketException == null ?
                    ex.GetAllMessages() :
                    ((SocketError)socketException.ErrorCode).ToString();
                ShowMessage(message);
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
                _ns.Close();
                _client.Close();
            }
            // Free any unmanaged objects here.
            disposed = true;
        }

        ~Talker() {
            Dispose(false);
        }

        #endregion

        #region INotifyPropertyChanged members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName = "") {
            var handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
