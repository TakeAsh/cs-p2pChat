﻿using System;
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
        INotifyPropertyChangedWithValue {

        private const int BufferSize = 1024;
        private static readonly char[] WhiteSpaces = new[] { ' ', '\n', '\r', '\t', '\0', };

        private bool disposed = false;
        private PropertyChangedWithValueEventHandler _propertyChangedWithValue;
        private TcpClient _client;
        private NetworkStream _ns;
        private BackgroundWorker _worker;
        private string _message = null;

        public Talker(
            string host,
            int port,
            int timeout,
            PropertyChangedWithValueEventHandler propertyChangedWithValue = null
        ) {
            Host = host;
            Port = port;
            Timeout = timeout;
            if (propertyChangedWithValue != null) {
                _propertyChangedWithValue = propertyChangedWithValue;
                this.PropertyChangedWithValue += propertyChangedWithValue;
            }
            _client = new TcpClient(Host, Port);
            _ns = _client.GetStream();
            if (_ns.CanTimeout) {
                _ns.ReadTimeout = Timeout * 1000;
                _ns.WriteTimeout = Timeout * 1000;
            }
            _worker = CreateWorker();
            this.NotifyPropertyChanged("Connected", true);
        }

        public string Host { get; private set; }
        public int Port { get; private set; }
        public int Timeout { get; private set; }

        public bool Connected {
            get { return _client.GetState() == TcpState.Established; }
        }

        public string Message {
            get { return _message; }
            private set {
                _message = value;
                this.NotifyPropertyChanged("Message", _message);
            }
        }

        public void Talk(ChatMessage message) {
            if (_ns == null || !_ns.CanWrite) {
                return;
            }
            var bytes = message.ToBytes();
            _ns.Write(bytes, 0, bytes.Length);
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
                this.NotifyPropertyChanged("Connected", false);
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
            worker.RunWorkerAsync();
            return worker;
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
                        ChatMessage.FromBytes(ms.GetBuffer()).Body;
                }
                Message = message;
            }
            catch (Exception ex) {
                var socketException = ex.InnerException as SocketException;
                var message = socketException == null ?
                    ex.GetAllMessages() :
                    ((SocketError)socketException.ErrorCode).ToString();
                Message = message;
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
            if (_propertyChangedWithValue != null) {
                this.PropertyChangedWithValue -= _propertyChangedWithValue;
                _propertyChangedWithValue = null;
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
